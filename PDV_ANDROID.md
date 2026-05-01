# PDV Android — Thin Client para Tablets

> Documentação de arquitetura e implementação do cliente Android para o PDV.
> O backend ASP.NET Core + PostgreSQL permanece em um servidor central na rede — o tablet é apenas uma tela.

---

## Visão Geral

```
Servidor central (fixo na rede local)
  ┌─────────────────────────────────────────┐
  │  ASP.NET Core + PostgreSQL              │
  │  nginx (porta 80/443)                   │
  │                                         │
  │  ┌── Impressora térmica (TCP)           │
  │  └── NF-e → SEFAZ (internet)           │
  └─────────────────────────────────────────┘
              │
              │  HTTP/HTTPS (rede local)
              │
  ┌───────────────────────┐   ┌───────────────────────┐
  │   Tablet Android #1   │   │   Tablet Android #2   │
  │   Caixa 1             │   │   Caixa 2             │
  │   WebView kiosk       │   │   WebView kiosk       │
  └───────────────────────┘   └───────────────────────┘
```

O tablet não executa lógica de negócio, não tem banco de dados local e não precisa de .NET instalado. É um browser em tela cheia travado no sistema.

---

## Componentes do App Android

### O que o app faz

- Abre o PDV em WebView fullscreen sem barra de endereço
- Ativa Lock Task Mode (kiosk) — botões de navegação, barra de status e acesso ao Android são bloqueados
- Mantém a tela sempre ligada (wake lock)
- Detecta perda de conexão e exibe tela de espera com retry automático
- Permite configurar o endereço do servidor na primeira inicialização
- Reinicia automaticamente se o app travar

### O que o app NÃO faz

- Processar pagamentos
- Gerenciar banco de dados
- Comunicar diretamente com impressoras (o servidor faz isso)
- Armazenar dados localmente

---

## Stack Técnica

| Componente | Tecnologia |
|-----------|-----------|
| Linguagem | Kotlin |
| Min SDK | Android 9 (API 28) |
| Target SDK | Android 14 (API 34) |
| UI | WebView nativo (sem framework extra) |
| Kiosk | Lock Task Mode (Device Owner API) |
| Build | Gradle + Android Studio |
| Distribuição | APK via sideload ou MDM |

---

## Estrutura do Projeto Android

```
mapos-pdv-android/
├── app/
│   ├── src/main/
│   │   ├── java/dev/barcelos/mapos/pdv/
│   │   │   ├── MainActivity.kt          # WebView + Lock Task Mode
│   │   │   ├── SetupActivity.kt         # Configuração inicial (IP do servidor)
│   │   │   ├── OfflineActivity.kt       # Tela de sem conexão
│   │   │   ├── DeviceAdminReceiver.kt   # Device Owner receiver
│   │   │   └── BootReceiver.kt          # Auto-start no boot
│   │   ├── res/
│   │   │   └── layout/
│   │   │       ├── activity_main.xml
│   │   │       └── activity_offline.xml
│   │   └── AndroidManifest.xml
│   └── build.gradle
├── device-owner/
│   └── setup-device-owner.sh            # Script de provisionamento
└── README.md
```

---

## Implementação

### MainActivity.kt

```kotlin
class MainActivity : AppCompatActivity() {

    private lateinit var webView: WebView
    private lateinit var wakeLock: PowerManager.WakeLock

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        // Tela sempre ligada
        val pm = getSystemService(POWER_SERVICE) as PowerManager
        wakeLock = pm.newWakeLock(PowerManager.SCREEN_BRIGHT_WAKE_LOCK, "mapos:pdv")
        wakeLock.acquire()

        // Fullscreen sem barra de status
        window.setFlags(
            WindowManager.LayoutParams.FLAG_FULLSCREEN,
            WindowManager.LayoutParams.FLAG_FULLSCREEN
        )

        webView = WebView(this)
        webView.settings.apply {
            javaScriptEnabled    = true
            domStorageEnabled    = true
            databaseEnabled      = true
            cacheMode            = WebSettings.LOAD_DEFAULT
            userAgentString      += " MapOsPdvAndroid/1.0"
        }

        // Interceptar erros de rede → tela offline
        webView.webViewClient = object : WebViewClient() {
            override fun onReceivedError(
                view: WebView, request: WebResourceRequest, error: WebResourceError
            ) {
                if (request.isForMainFrame) mostrarOffline()
            }
        }

        setContentView(webView)

        // Ativar kiosk
        val dpm = getSystemService(DEVICE_POLICY_SERVICE) as DevicePolicyManager
        val admin = ComponentName(this, DeviceAdminReceiver::class.java)
        if (dpm.isDeviceOwnerApp(packageName)) {
            dpm.setLockTaskPackages(admin, arrayOf(packageName))
            startLockTask()
        }

        carregarPdv()
    }

    private fun carregarPdv() {
        val prefs  = getSharedPreferences("pdv", MODE_PRIVATE)
        val url    = prefs.getString("server_url", null)

        if (url.isNullOrBlank()) {
            startActivity(Intent(this, SetupActivity::class.java))
            return
        }

        webView.loadUrl("$url/Pdv/AbrirCaixa")
    }

    private fun mostrarOffline() {
        webView.visibility = View.GONE
        // exibir layout de reconexão com countdown
        Handler(Looper.getMainLooper()).postDelayed({ carregarPdv() }, 5_000)
    }

    override fun onDestroy() {
        super.onDestroy()
        if (wakeLock.isHeld) wakeLock.release()
    }

    // Bloquear botão voltar (não fecha o app)
    override fun onBackPressed() { /* bloqueado */ }
}
```

### SetupActivity.kt — configuração inicial

Exibida apenas na primeira abertura ou quando o servidor não responde após 3 tentativas:

```kotlin
// Tela simples com:
// - Campo de texto: endereço do servidor (ex: http://192.168.1.10)
// - Botão "Testar conexão" → GET /health
// - Botão "Salvar"

fun salvarEIniciar() {
    val url = binding.inputUrl.text.toString().trimEnd('/')
    lifecycleScope.launch {
        try {
            val resp = URL("$url/health").readText()  // endpoint de health check
            getSharedPreferences("pdv", MODE_PRIVATE)
                .edit().putString("server_url", url).apply()
            startActivity(Intent(this@SetupActivity, MainActivity::class.java))
            finish()
        } catch (e: Exception) {
            binding.erro.text = "Não foi possível conectar ao servidor."
        }
    }
}
```

### DeviceAdminReceiver.kt

```kotlin
class DeviceAdminReceiver : DeviceAdminReceiver() {
    override fun onEnabled(context: Context, intent: Intent) {
        // Device Owner ativado
    }
}
```

### BootReceiver.kt — iniciar automaticamente no boot

```kotlin
class BootReceiver : BroadcastReceiver() {
    override fun onReceive(context: Context, intent: Intent) {
        if (intent.action == Intent.ACTION_BOOT_COMPLETED) {
            context.startActivity(
                Intent(context, MainActivity::class.java)
                    .addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
            )
        }
    }
}
```

### AndroidManifest.xml — permissões necessárias

```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.WAKE_LOCK" />
<uses-permission android:name="android.permission.RECEIVE_BOOT_COMPLETED" />
<uses-permission android:name="android.permission.DISABLE_KEYGUARD" />

<application android:usesCleartextTraffic="true"> <!-- para HTTP local -->

    <activity android:name=".MainActivity"
              android:launchMode="singleTask"
              android:screenOrientation="landscape">
        <intent-filter>
            <action android:name="android.intent.action.MAIN" />
            <category android:name="android.intent.category.LAUNCHER" />
        </intent-filter>
    </activity>

    <receiver android:name=".DeviceAdminReceiver"
              android:permission="android.permission.BIND_DEVICE_ADMIN">
        <meta-data android:name="android.app.device_admin"
                   android:resource="@xml/device_admin" />
        <intent-filter>
            <action android:name="android.app.action.DEVICE_ADMIN_ENABLED" />
        </intent-filter>
    </receiver>

    <receiver android:name=".BootReceiver">
        <intent-filter>
            <action android:name="android.intent.action.BOOT_COMPLETED" />
        </intent-filter>
    </receiver>

</application>
```

---

## Provisionamento — Ativar Device Owner

O Lock Task Mode (kiosk completo) exige que o app seja **Device Owner**. Isso precisa ser feito **uma única vez por tablet**, com o dispositivo em estado de fábrica ou recém restaurado.

### Opção 1 — Via ADB (desenvolvimento / pequena escala)

```bash
# Conectar tablet via USB com ADB habilitado
adb install mapos-pdv.apk

adb shell dpm set-device-owner \
    dev.barcelos.mapos.pdv/.DeviceAdminReceiver
```

Após isso, o ADB pode ser desabilitado e o tablet opera em kiosk permanentemente.

### Opção 2 — Via QR Code (escala, sem ADB)

No primeiro boot (ou após factory reset), o Android permite provisionar um Device Owner via QR Code. O QR Code é gerado a partir de um JSON de configuração:

```json
{
  "android.app.extra.PROVISIONING_DEVICE_ADMIN_COMPONENT_NAME":
    "dev.barcelos.mapos.pdv/.DeviceAdminReceiver",

  "android.app.extra.PROVISIONING_DEVICE_ADMIN_PACKAGE_DOWNLOAD_LOCATION":
    "https://releases.barcelos.dev/mapos-pdv.apk",

  "android.app.extra.PROVISIONING_DEVICE_ADMIN_PACKAGE_CHECKSUM":
    "<SHA-256 do APK>",

  "android.app.extra.PROVISIONING_SKIP_ENCRYPTION": true,
  "android.app.extra.PROVISIONING_WIFI_SSID": "RedeDoEstabelecimento",
  "android.app.extra.PROVISIONING_WIFI_PASSWORD": "senha_da_rede"
}
```

Ferramentas para gerar o QR: [Android Provisioning](https://provisioning.googleusercontent.com/provisioning/so/2Rru...) ou qualquer gerador de QR com o JSON acima.

**Fluxo para o cliente:**
1. Liga o tablet recém resetado
2. Na tela de boas-vindas, toca 6 vezes rapidamente na tela
3. Escaneia o QR Code
4. Tablet configura Wi-Fi, instala o app e ativa kiosk automaticamente
5. Tela de configuração do servidor aparece — digita o IP
6. PDV abre

### Opção 3 — NFC (escala com NFC Tag)

Similar ao QR, mas o administrador encostar uma NFC tag no tablet no primeiro boot. Útil para lojas com muitos tablets.

---

## Saindo do Kiosk (acesso administrativo)

O operador não consegue sair do kiosk. Para acesso administrativo (atualização, manutenção):

```kotlin
// Botão oculto: toque longo de 5 segundos no logo da tela de login
binding.logo.setOnLongClickListener {
    val dialog = AlertDialog.Builder(this)
    dialog.setTitle("Acesso administrativo")
    dialog.setMessage("Digite a senha de manutenção:")
    // ... valida senha e chama stopLockTask()
    true
}
```

A senha de saída do kiosk é diferente da senha do PDV — configurada no provisionamento.

---

## Endpoint de Health Check no servidor

O app de setup precisa validar o servidor. Adicionar ao backend:

```csharp
// Program.cs
app.MapGet("/health", () => Results.Ok(new { status = "ok", version = "1.0" }))
   .AllowAnonymous();
```

---

## Hardware Recomendado

### Tablets homologados

| Modelo | RAM | Armazenamento | Notas |
|--------|-----|---------------|-------|
| Samsung Galaxy Tab A9 | 4 GB | 64 GB | Melhor suporte Android, atualização garantida |
| Lenovo Tab M10 Plus | 4 GB | 64 GB | Boa relação custo-benefício |
| Positivo Twist Pad | 4 GB | 64 GB | Nacional, suporte local |
| Multilaser M10A Pro | 3 GB | 32 GB | Opção econômica |

**Requisitos mínimos:** Android 9+, 3 GB RAM, tela 10", Wi-Fi 5 GHz.

### Suporte físico

- Suporte de mesa com fonte embutida (tablet sempre carregando)
- Suporte articulado para balcão (cliente assinar na tela)
- Case reforçado para ambiente de cozinha/estoque

### Impressoras homologadas (rede TCP)

| Modelo | Protocolo | Notas |
|--------|-----------|-------|
| Epson TM-T20X | TCP/USB | Mais compatível com ESC/POS |
| Bematech MP-4200 TH | TCP/USB | Muito usada no Brasil |
| Elgin i9 | TCP | Nacional, boa relação custo |
| Daruma DR800 | TCP | Nacional |

Impressoras na rede são conectadas ao servidor, não ao tablet. O tablet não precisa saber da impressora.

---

## Configuração de Rede

### Endereço IP fixo no servidor

O servidor do sistema deve ter IP fixo na rede local (ou reserva DHCP pelo MAC address no roteador):

```
Servidor: 192.168.1.10 (fixo)
Tablets:  192.168.1.50–100 (DHCP)
```

### DNS local (opcional mas recomendado)

Em vez de IP, configurar DNS local para `pdv.loja.local`:

```
# /etc/hosts no roteador ou servidor DNS local
192.168.1.10    pdv.loja.local
```

O app conecta em `http://pdv.loja.local` — se o IP do servidor mudar, só atualiza o DNS.

### Segurança de rede

- O PDV não precisa de acesso à internet no tablet — apenas rede local
- VLAN separada para os tablets (opcional, recomendado para ambiente maior)
- HTTPS com certificado autoassinado para comunicação tablet↔servidor

---

## Atualização do App

### Opção A — Sideload manual (pequena escala)

```bash
# Desabilitar kiosk temporariamente via senha administrativa
# Conectar ADB
adb install -r mapos-pdv-nova-versao.apk
# Kiosk volta automaticamente no próximo boot
```

### Opção B — Auto-update via servidor (recomendado)

O app verifica periodicamente se há nova versão disponível:

```kotlin
// Verificar versão a cada 24h
suspend fun verificarAtualizacao() {
    val resp = URL("http://pdv.loja.local/api/versao-android").readText()
    val versaoServidor = JSONObject(resp).getString("versao")
    if (versaoServidor > BuildConfig.VERSION_NAME) {
        baixarEInstalarApk(resp) // requer permissão INSTALL_PACKAGES (Device Owner tem)
    }
}
```

Endpoint no servidor:

```csharp
app.MapGet("/api/versao-android", () => Results.Ok(new {
    versao = "1.2.0",
    url    = "https://releases.barcelos.dev/mapos-pdv-1.2.0.apk",
    sha256 = "abc123..."
})).AllowAnonymous();
```

---

## Checklist de implantação

```
Servidor
[ ] IP fixo configurado no roteador
[ ] Sistema acessível em http://IP/
[ ] Endpoint /health respondendo
[ ] Impressoras na rede funcionando

Tablet (por unidade)
[ ] Factory reset realizado
[ ] Provisionamento Device Owner (QR Code ou ADB)
[ ] IP do servidor configurado no app
[ ] Tela de abertura de caixa carregando
[ ] Tela sempre ligada (wake lock ativo)
[ ] Botões de navegação Android invisíveis
[ ] Teste de reconexão: desligar Wi-Fi e religar
[ ] Suporte físico instalado com cabo de energia
```

---

## Repositório

O código do app Android será mantido em repositório separado:

**`github.com/allanbarcelos/mapos-pdv-android`**

O backend (este repositório) não requer nenhuma alteração para suportar os tablets — qualquer browser já acessa o sistema normalmente.

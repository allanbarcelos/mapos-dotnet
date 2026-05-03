# Hardware Homologado para PDV

Guia de recomendação de impressoras térmicas e leitores de código de barras compatíveis com o sistema. Todos os modelos listados foram selecionados com base em compatibilidade com ESC/POS, disponibilidade no mercado brasileiro e custo-benefício.

---

## Impressoras Térmicas

O sistema se comunica com impressoras via **ESC/POS sobre socket TCP** (rede) ou **USB**. Impressoras na rede são fortemente recomendadas — o servidor conecta diretamente, sem depender do computador ou tablet do operador.

### Como o sistema se conecta

```
Servidor (ASP.NET Core)
    └── TCP socket → IP da impressora : porta 9100
```

A porta padrão ESC/POS é **9100**. Nenhum driver é necessário para impressoras em rede.

---

### 1. Elgin i9

**Recomendação principal para novos projetos.**

| Especificação | Detalhe |
|---------------|---------|
| Velocidade | 250 mm/s |
| Largura do papel | 80 mm |
| Resolução | 203 dpi |
| Conexões | USB + Ethernet (TCP) + Serial + Bluetooth |
| Guilhotina | Automática parcial |
| Autocutter | Sim |
| Compatibilidade | ESC/POS total |
| Garantia | 1 ano (nacional) |
| Faixa de preço | R$ 550 – R$ 750 |

**Por que indicar:** fabricada e suportada no Brasil, peças e assistência acessíveis, todas as interfaces disponíveis, excelente velocidade para o preço.

**Configuração de rede:** acessar o painel via botão interno, imprimir página de configuração, usar o IP impresso. Configurar IP fixo via utilitário Windows da Elgin ou direto no roteador (reserva DHCP por MAC).

---

### 2. Bematech MP-4200 TH

**Clássico do varejo brasileiro. Altíssima disponibilidade de assistência técnica.**

| Especificação | Detalhe |
|---------------|---------|
| Velocidade | 200 mm/s |
| Largura do papel | 80 mm |
| Resolução | 203 dpi |
| Conexões | USB + Ethernet (TCP) + Serial |
| Guilhotina | Automática parcial |
| Compatibilidade | ESC/POS total |
| Garantia | 1 ano (nacional) |
| Faixa de preço | R$ 650 – R$ 950 |

**Por que indicar:** padrão de facto em muitos estabelecimentos brasileiros, ampla base de técnicos especializados, robusta para uso intenso.

**Observação:** modelos mais antigos (MP-2100, MP-2500) possuem apenas serial/USB — verificar antes de comprar se precisa de Ethernet.

---

### 3. Epson TM-T20X

**Padrão internacional. Melhor compatibilidade ESC/POS do mercado.**

| Especificação | Detalhe |
|---------------|---------|
| Velocidade | 250 mm/s |
| Largura do papel | 80 mm |
| Resolução | 203 dpi |
| Conexões | USB + Ethernet (TCP) |
| Guilhotina | Automática total |
| Compatibilidade | ESC/POS referência (Epson é o criador do padrão) |
| Garantia | 1 ano |
| Faixa de preço | R$ 750 – R$ 1.100 |

**Por que indicar:** a Epson criou o protocolo ESC/POS — compatibilidade perfeita e garantida. Guilhotina total (corte completo) é mais conveniente. Durabilidade comprovada em ambiente de alto volume.

**Observação:** ligeiramente mais cara que concorrentes nacionais, mas raramente apresenta problemas de compatibilidade.

---

### 4. Tanca TP-650

**Melhor custo-benefício da categoria.**

| Especificação | Detalhe |
|---------------|---------|
| Velocidade | 250 mm/s |
| Largura do papel | 80 mm |
| Resolução | 203 dpi |
| Conexões | USB + Ethernet (TCP) + Serial + Bluetooth |
| Guilhotina | Automática parcial |
| Compatibilidade | ESC/POS total |
| Garantia | 1 ano (nacional) |
| Faixa de preço | R$ 400 – R$ 600 |

**Por que indicar:** preço abaixo da média com especificações equivalentes aos modelos premium. Boa opção para implantações com múltiplos pontos de venda onde o orçamento é relevante.

---

### 5. Daruma DR800

**Boa opção para quem já usa Daruma em outros equipamentos fiscais.**

| Especificação | Detalhe |
|---------------|---------|
| Velocidade | 200 mm/s |
| Largura do papel | 80 mm |
| Resolução | 203 dpi |
| Conexões | USB + Ethernet (TCP) + Serial |
| Guilhotina | Automática parcial |
| Compatibilidade | ESC/POS total |
| Garantia | 1 ano (nacional) |
| Faixa de preço | R$ 500 – R$ 800 |

**Por que indicar:** marca com longa presença no mercado fiscal brasileiro, rede de assistência consolidada, confiável para uso contínuo.

---

### 6. Sweda SI-300

**Opção econômica para baixo volume de impressão.**

| Especificação | Detalhe |
|---------------|---------|
| Velocidade | 150 mm/s |
| Largura do papel | 80 mm |
| Resolução | 203 dpi |
| Conexões | USB + Serial |
| Guilhotina | Automática parcial |
| Compatibilidade | ESC/POS total |
| Garantia | 1 ano (nacional) |
| Faixa de preço | R$ 300 – R$ 450 |

**Por que indicar:** menor custo inicial, adequada para estabelecimentos com baixo volume diário (< 100 cupons/dia). Não possui Ethernet — usar USB conectada ao servidor ou computador local.

**Observação:** sem interface de rede, adequada apenas quando a impressora está fisicamente próxima ao servidor.

---

### Comparativo rápido — Impressoras

| Modelo | Velocidade | Rede TCP | Bluetooth | Guilhotina | Preço aprox. |
|--------|-----------|----------|-----------|------------|-------------|
| Elgin i9 | 250 mm/s | ✅ | ✅ | Parcial | R$ 550–750 |
| Bematech MP-4200 TH | 200 mm/s | ✅ | ❌ | Parcial | R$ 650–950 |
| Epson TM-T20X | 250 mm/s | ✅ | ❌ | **Total** | R$ 750–1.100 |
| Tanca TP-650 | 250 mm/s | ✅ | ✅ | Parcial | R$ 400–600 |
| Daruma DR800 | 200 mm/s | ✅ | ❌ | Parcial | R$ 500–800 |
| Sweda SI-300 | 150 mm/s | ❌ | ❌ | Parcial | R$ 300–450 |

**Recomendação por cenário:**

| Cenário | Modelo |
|---------|--------|
| Melhor custo-benefício geral | Tanca TP-650 |
| Maior confiabilidade e suporte | Epson TM-T20X |
| Múltiplos caixas (orçamento) | Tanca TP-650 |
| Estabelecimento com técnico Bematech local | Bematech MP-4200 TH |
| Baixíssimo volume / orçamento restrito | Sweda SI-300 |

---

## Leitores de Código de Barras

O sistema PDV recebe o código de barras como entrada de teclado — todos os leitores USB funcionam como **HID keyboard** e não requerem driver ou configuração adicional. Basta plugar e usar.

Para tablets Android, leitores Bluetooth também funcionam nativamente.

### Como o sistema usa o leitor

```
Leitor escaneia → envia código como digitação de teclado →
campo de busca do PDV captura → produto encontrado automaticamente
```

O campo de busca do PDV já monitora a entrada e identifica automaticamente quando o input vem de um leitor (velocidade de digitação > humana).

---

### 1. Zebra DS2208

**Melhor custo-benefício da categoria. Referência de confiabilidade.**

| Especificação | Detalhe |
|---------------|---------|
| Tipo | 1D + 2D (QR Code, DataMatrix, PDF417) |
| Interface | USB-HID |
| Velocidade de leitura | 100 scans/s |
| Distância de leitura | Até 30 cm |
| Queda | Suporta até 1,5 m |
| Garantia | 3 anos |
| Faixa de preço | R$ 350 – R$ 550 |

**Por que indicar:** lê 1D e 2D, suporta códigos danificados ou com baixo contraste, durabilidade comprovada em varejo intenso. Marca com presença consolidada no Brasil.

---

### 2. Honeywell Voyager 1250g

**O mais vendido no mundo em leitores 1D. Simples, rápido, indestrutível.**

| Especificação | Detalhe |
|---------------|---------|
| Tipo | 1D (laser) |
| Interface | USB-HID |
| Velocidade de leitura | 100 scans/s |
| Distância de leitura | Até 35 cm |
| Queda | Suporta até 1,5 m |
| Garantia | 3 anos |
| Faixa de preço | R$ 250 – R$ 400 |

**Por que indicar:** se o estabelecimento usa apenas códigos de barras lineares (EAN-13, EAN-8, Code128), este é o mais eficiente. Leitura laser é mais rápida e precisa em 1D do que imagem. Manutenção zero.

---

### 3. Datalogic QuickScan QD2430

**Leitura 2D de alto desempenho. Melhor para QR Code.**

| Especificação | Detalhe |
|---------------|---------|
| Tipo | 1D + 2D (imager) |
| Interface | USB-HID |
| Velocidade de leitura | 280 scans/s (superior à concorrência) |
| Distância de leitura | Até 40 cm |
| Queda | Suporta até 1,5 m |
| Garantia | 3 anos |
| Faixa de preço | R$ 350 – R$ 500 |

**Por que indicar:** melhor desempenho de leitura 2D da faixa de preço, excelente para ambientes com iluminação variável. Indicado se o estabelecimento usa QR Codes (NF-e, PIX, etc).

---

### 4. Elgin CM-500

**Custo mínimo com desempenho adequado. Ideal para implantações em escala.**

| Especificação | Detalhe |
|---------------|---------|
| Tipo | 1D + 2D |
| Interface | USB-HID |
| Velocidade de leitura | 100 scans/s |
| Distância de leitura | Até 25 cm |
| Queda | Suporta até 1,0 m |
| Garantia | 1 ano (nacional) |
| Faixa de preço | R$ 150 – R$ 250 |

**Por que indicar:** preço significativamente abaixo dos importados com funcionalidade equivalente para uso padrão. Marca com suporte nacional. Ideal para equipar múltiplos caixas com orçamento limitado.

---

### 5. Bematech BC-55 USB

**Confiável e amplamente disponível no Brasil.**

| Especificação | Detalhe |
|---------------|---------|
| Tipo | 1D |
| Interface | USB-HID |
| Velocidade de leitura | 100 scans/s |
| Distância de leitura | Até 30 cm |
| Queda | Suporta até 1,5 m |
| Garantia | 1 ano (nacional) |
| Faixa de preço | R$ 180 – R$ 300 |

**Por que indicar:** marca já conhecida do ecossistema de quem usa Bematech em impressoras. Facilita a compra em um único fornecedor. Confiável para uso diário.

---

### 6. Honeywell Xenon 1900 (sem fio)

**Melhor opção sem fio para tablets ou balcões maiores.**

| Especificação | Detalhe |
|---------------|---------|
| Tipo | 1D + 2D (imager) |
| Interface | USB-HID (base dongle) + Bluetooth |
| Alcance sem fio | Até 10 m da base |
| Velocidade de leitura | 100 scans/s |
| Bateria | Li-Ion recarregável (8h de uso) |
| Queda | Suporta até 1,8 m |
| Garantia | 3 anos |
| Faixa de preço | R$ 600 – R$ 900 |

**Por que indicar:** liberdade de movimento sem cabo — ideal para balcões de checkout onde o operador precisa escanear produtos de formatos e tamanhos variados. Compatível com tablets Android via Bluetooth HID.

---

### Comparativo rápido — Leitores

| Modelo | 1D | 2D | Sem fio | Garantia | Preço aprox. |
|--------|----|----|---------|----------|-------------|
| Zebra DS2208 | ✅ | ✅ | ❌ | 3 anos | R$ 350–550 |
| Honeywell Voyager 1250g | ✅ | ❌ | ❌ | 3 anos | R$ 250–400 |
| Datalogic QD2430 | ✅ | ✅ | ❌ | 3 anos | R$ 350–500 |
| Elgin CM-500 | ✅ | ✅ | ❌ | 1 ano | R$ 150–250 |
| Bematech BC-55 | ✅ | ❌ | ❌ | 1 ano | R$ 180–300 |
| Honeywell Xenon 1900 | ✅ | ✅ | **✅** | 3 anos | R$ 600–900 |

**Recomendação por cenário:**

| Cenário | Modelo |
|---------|--------|
| Uso geral com melhor custo-benefício | Zebra DS2208 |
| Apenas código de barras linear, alto volume | Honeywell Voyager 1250g |
| Estabelecimento com QR Code / 2D | Datalogic QD2430 |
| Múltiplos caixas com orçamento limitado | Elgin CM-500 |
| Tablet Android ou balcão grande (sem fio) | Honeywell Xenon 1900 |

---

## Kit PDV Recomendado

### Kit Econômico (~R$ 600–900)

| Item | Modelo | Preço |
|------|--------|-------|
| Impressora | Tanca TP-650 | R$ 400–600 |
| Leitor | Elgin CM-500 | R$ 150–250 |

### Kit Profissional (~R$ 1.000–1.500)

| Item | Modelo | Preço |
|------|--------|-------|
| Impressora | Elgin i9 | R$ 550–750 |
| Leitor | Zebra DS2208 | R$ 350–550 |

### Kit Premium (~R$ 1.800–2.500)

| Item | Modelo | Preço |
|------|--------|-------|
| Impressora | Epson TM-T20X | R$ 750–1.100 |
| Leitor sem fio | Honeywell Xenon 1900 | R$ 600–900 |

---

## Onde comprar (Brasil)

- **Americanas Empresas / B2W** — boa variedade, entrega rápida
- **Mercado Livre** — preços competitivos, verificar reputação do vendedor
- **Site oficial Elgin** — elgin.com.br — venda direta com suporte
- **Site oficial Bematech** — bematech.com.br
- **Lojas especializadas em automação comercial** — atendimento técnico, instalação
- **AliExpress** (impressoras genéricas) — apenas para testes, não recomendado para produção

---

## Observações Gerais

**Papel térmico:** usar sempre papel de 80 mm × 65 g/m². Papel de qualidade inferior ou espessura errada causa atolamentos e reduz a vida útil do cabeçote.

**Cabo de rede:** impressoras em rede devem ter cabo CAT5e ou superior direto no switch — evitar Wi-Fi para impressoras (latência e instabilidade).

**Atualização de firmware:** impressoras Epson e Zebra disponibilizam firmware atualizado nos sites oficiais. Manter atualizado evita problemas de compatibilidade.

**Garantia:** preferir modelos com garantia de 3 anos para uso em produção intenso. O custo de manutenção após a garantia pode superar o preço de um novo equipamento de entrada.

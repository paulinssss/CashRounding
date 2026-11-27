# CashRounding — SyrveFront POS Plugin (API V8)

Plugin per **SyrveFront POS (API V8 / .NET Framework 4.7.2)** che esegue l'arrotondamento **per difetto in favore del cliente** al multiplo di **0,05** (eliminando le monete da 1 e 2 centesimi) tramite uno **sconto libero a importo** configurato in Syrve BackOffice.

Lingue supportate: **Italiano** e **Inglese** (rilevamento automatico dalla lingua di SyrveFront).

---

## Esempi di arrotondamento / Rounding examples
- `0,17` → sconto `0,02` → totale `0,15`
- `0,24` → sconto `0,04` → totale `0,20`
- `1,01` → sconto `0,01` → totale `1,00`
- `1,05` → sconto `0,00` → totale `1,05`

---

## Flusso operativo lato POS / POS Workflow
1. **Pulsante dedicato sulla Schermata di Pagamento** (`AddButtonToPaymentScreen`):
   - Mostra dinamicamente lo sconto applicabile (es. `Arrotonda (-0,02)`).
   - Con **1 tocco** applica o rimuove lo sconto libero sull'ordine prima dell'inserimento del pagamento Contanti (stato selezionato `Arrotondato (-0,02)`).
   - Se l'ordine ha già un preconto stampato (`Bill`), chiede conferma prima di annullare il preconto e applicare l'arrotondamento.
   - Se l'operatore aveva già inserito uno o più pagamenti non ancora processati prima di premere il tasto di arrotondamento, tali pagamenti vengono **azzerati automaticamente** così da evitare importi versati superiori al nuovo totale e richiedere il reinserimento del pagamento.
   - Se il totale è già multiplo di `0,05`, il pulsante mostra `Già arrotondato` disabilitato.
2. **Modalità automatica opzionale all'ingresso in pagamento** (`AutoApplyOnEnterPaymentScreen`):
   - Di default `false` (i pagamenti Carta restano invariati; sui Contanti si usa il tasto dedicato).
   - Se impostata a `true` (anche dal menu Plugin POS), applica automaticamente l'arrotondamento su `NavigatingToPaymentScreen` all'ingresso in cassa, e il tasto sulla schermata di pagamento consente di rimuoverlo con 1 tocco in caso di pagamento con Carta.

---

## Configurazione in Syrve BackOffice / BackOffice Setup
1. In **Syrve Office**, vai su **Sconti e Maggiorazioni** (*Discounts and Surcharges*).
2. Crea un nuovo sconto attivo (es. `Arrotondamento`).
3. Abilita l'opzione **Importo libero / Flexible sum** (`DiscountByFlexibleSum = true`) e l'applicazione manuale.
4. Salva e attendi la sincronizzazione con SyrveFront.

---

## Installazione / Installation
1. Compila in Release:
   ```powershell
   dotnet build CashRounding.csproj -c Release
   ```
2. Copia la cartella `bin\Release\Resto.Front.Api.CashRounding.V8\` nella cartella `Plugins` di SyrveFront.
3. Avvia SyrveFront e apri il menu **Plugin** → **Arrotondamento Contanti** (*Cash Rounding*) → **Seleziona sconto arrotondamento** (*Select rounding discount*).
   - L'elenco mostra **esclusivamente** gli sconti attivi con importo libero (`DiscountByFlexibleSum`).
   - La selezione viene salvata automaticamente in `CashRounding.dll.config` senza riavviare il POS.

---

## Parametri `CashRounding.dll.config`
- `Enabled`: `true` / `false` (abilita o disabilita il plugin).
- `AutoApplyOnEnterPaymentScreen`: `false` / `true` (se `true`, applica automaticamente lo sconto su `NavigatingToPaymentScreen`).
- `DiscountTypeId`: GUID dello sconto libero (impostato automaticamente dal menu Plugin; ha priorità sul nome).
- `DiscountTypeName`: nome dello sconto libero in Syrve BackOffice (fallback se `DiscountTypeId` è vuoto).
- `RoundingStep`: passo di arrotondamento (default `0.05`).
- `ShowNotification`: `true` / `false` (mostra un toast discreto quando l'arrotondamento viene applicato/rimosso).

# Changelog

## [1.2.0] - 2026-11-27
### Aggiunto
- Azzeramento automatico di eventuali righe di pagamento non ancora processate (`PaymentStatus.New`, esclusi prepagamenti e pagamenti già processati/fiscalizzati esternamente) quando l'operatore applica o rimuove l'arrotondamento tramite il pulsante sulla schermata di pagamento, evitando importi dovuti disallineati e richiedendo il reinserimento del pagamento sul nuovo totale.
- Messaggi di notifica dedicati (IT/EN) quando i pagamenti inseriti vengono azzerati.

## [1.1.0] - 2026-11-27
### Aggiunto
- Pulsante toggle dedicato sulla schermata di pagamento (`AddButtonToPaymentScreen`) con stato dinamico (`Arrotonda (-0,02)` / `Arrotondato (-0,02)` / `Già arrotondato`) per applicare o rimuovere l'arrotondamento con 1 tocco prima di inserire il pagamento.
- Impostazione `AutoApplyOnEnterPaymentScreen` (default `false`, attivabile anche dal menu Plugin) per applicare automaticamente l'arrotondamento all'ingresso nella schermata di pagamento (`NavigatingToPaymentScreen`).
- Gestione esplicita degli ordini con preconto già stampato (`OrderStatus.Bill`) con conferma operatore per annullare il preconto se richiesto.

### Corretto
- Risolta `ConstraintViolationException` su `OrderStatus.Bill` rimuovendo la modifica dell'ordine in `BeforeDoCheque` (dove Syrve ha già portato l'ordine in stato `Bill`) e spostandola su `AddButtonToPaymentScreen` e `NavigatingToPaymentScreen`.

## [1.0.1] - 2026-11-27
### Corretto
- Risolta l'eccezione `EntityAlreadyInUseException` durante l'applicazione dello sconto sulla schermata di pagamento.

### Aggiunto
- Versionamento esplicito nel `.csproj` e log della versione dell'assembly all'avvio del plugin.

## [1.0.0] - 2026-11-27
### Aggiunto
- Prima release del plugin `CashRounding` (API V8 / `.NET Framework 4.7.2`) per arrotondamento per difetto al multiplo di `0,05`.
- Pulsante nel menu Plugin POS touchscreen per selezionare esclusivamente gli sconti attivi con `DiscountByFlexibleSum == true`.
- Supporto bilingue Italiano / Inglese con aggiornamento dinamico su `CurrentCultureChanged`.

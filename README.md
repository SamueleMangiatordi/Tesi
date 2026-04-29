FIRE PROTOCOL

Simulatore di Protocollo antincendio in Realtà Virtuale
---

## Requisiti di Sistema

Prima di iniziare, assicurati di avere installato:

* **Unity Hub:** 
* **Unity Editor:** versione utilizzata: 6000.2.6f2

---

## Come Iniziare

Segui questi passaggi per scaricare e avviare il progetto correttamente:

1.  **Clona il Repository:**
    ```bash
    git clone https://github.com/SamueleMangiatordi/Tesi
    ```
2.  **Apri Unity Hub:** Clicca su **Add** (Aggiungi) e seleziona la cartella principale del repository scaricato.
3.  **Selezione Editor:** Assicurati che la versione dell'editor indicata in Unity Hub corrisponda a quella del progetto.
4.  **Primo Avvio:** La prima apertura richiederà qualche minuto. Unity genererà automaticamente la cartella `Library`, importando tutti gli asset.

---

## Istruzioni per la VR (Meta Quest)

Questo progetto è ottimizzato per **Meta Quest 3**. 

### Opzione A: Test rapido tramite Unity Editor (Link)
1. Installa l'app **Meta Quest Link** sul tuo PC.
2. Collega il visore al PC tramite cavo USB-C o Air Link.
2. Premi **Play** dalla scena "Menu".

### Opzione B: Installazione Standalone (APK)
1. Scarica .apk tramite la sezione "Releases" di GitHub.
2. Usa **SideQuest** o **Meta Quest Developer Hub** per installare l'APK sul visore.
3. Nel visore, vai in **Libreria App**, clicca sulla barra di ricerca e seleziona **Fonti Sconosciute** dal menu a tendina.
4. Avvia `TESI`.

## Struttura del Progetto

Ecco una panoramica di dove trovare i file principali:

* `Assets/Scenes`: Contiene le scene del gioco.
* `Assets/Scripts`: Tutti i file C# organizzati per funzionalità.
---


## Note per lo Sviluppo (Best Practices)

* **Punto di Ingresso:** Il gioco deve sempre essere avviato dalla scena `Menu` per inizializzare correttamente i Singleton.
* **Branch


open System.Drawing

// tipi di messaggio
type MESSAGE =
    | LINK_ALREADY_EXISTS
    | MSG_TEXT_TOO_SHORT
    | MSG_BAD_FORMAT
    | WRONG_NODES

type AlertBox( width:float32, height:float32 ) =
    // il valore alpha del colore
    let mutable alpha = 0
    // il tipo di messaggio
    let mutable message = ""
    // dimensioni della finestra
    let mutable width, height = width, height
    // derata della scritta
    let mutable time, timer = 3000, 3000
    // determina se la casella e' aperta
    let mutable isOpen = false
    
    member this.Open = isOpen
    
    // imposta il tipo di errore
    member this.setMessage( msg:MESSAGE, w_width:float32, w_height:float32 ) =
        match msg with
            | LINK_ALREADY_EXISTS -> message <- "ARCO GIA' ESISTENTE"
            | MSG_TEXT_TOO_SHORT -> message <- "NOME TROPPO CORTO: INSERIRE ALMENO UN CARATTERE"
            | WRONG_NODES -> message <- "NUMERO DI NODI SELEZIONATI ERRATO: SOLO 2 ALLA VOLTA"
            | MSG_BAD_FORMAT -> message <- "INSERIRE ALMENO UN CARATTERE DIVERSO DALLO SPAZIO"
        width <- w_width
        height <- w_height
        isOpen <- true
        timer <- time

    // aggiorna la schermata
    member this.update( delta:int ) =
        timer <- timer - delta
        if timer <= 0 then
            timer <- time
            isOpen <- false
        else
            if timer >= time * 2/3 then
                alpha <- min 255 (alpha + 10)
            else
                if timer < time / 3 then
                    alpha <- max 0 (alpha - 10)

    // disegna l'oggetto
    member this.paint( f:Font, g:Graphics ) =
        if isOpen then
            let sz = g.MeasureString( message, f )
            let offset = 10.f
            let x, y, w, h  = width / 2.f - sz.Width / 2.f - offset, height / 2.f - sz.Height / 2.f - offset, sz.Width + 2.f * offset, sz.Height + 2.f * offset
            let rect = new RectangleF( x, y, w, h )
            use b = new SolidBrush( Color.FromArgb( alpha, 0, 0, 0 ) )
            g.FillRectangle( b, rect )
            b.Color <- Color.FromArgb( alpha, 255, 255, 255 )
            g.DrawString( message, f, b, new RectangleF( x + offset, y + offset, w - offset, h - offset ) )
            use p = new Pen( Color.FromArgb( alpha, 255, 0, 0 ), 2.f )
            g.DrawRectangle( p, x, y, w, h )
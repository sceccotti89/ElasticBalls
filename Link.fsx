
open System.Drawing

type Link( f:Font, X1:float32, Y1:float32, X2:float32, Y2:float32, index1:int, index2:int ) =
    // posizione iniziali del nodo
    let mutable startX1, startY1, startX2, startY2 = X1, Y1, X2, Y2
    // coordinate del collegamento tra nodi
    let mutable x1, y1, x2, y2 = X1, Y1, X2, Y2
    // il nome associato al link
    let mutable name = ""
    // costante elastica
    let k = 3.f

    // restituisce gli indici a cui e' associato
    member this.Indexes = index1, index2

    // restituisce l'altro indice del nodo collegato
    member this.otherIndex( index:int ) =
        if index1 <> index then index1
        else index2

    // assegna il nome al collegamento
    member this.setName( value:string ) =
        name <- value

    ///<summary> modifica la posizione di uno dei due estremi </summary>
    ///<param name = "index"> indice del nodo che si e' mosso </param>
    ///<param name = "x"> nuova coordinata X da assegnare </param>
    ///<param name = "y"> nuova coordinata Y da assegnare </param>
    member this.setPosition( index:int, x:float32, y:float32, move:bool ) =
        if index = index1 then
            x1 <- x
            y1 <- y
            if not move then
                startX1 <- x
                startY1 <- y
        else
            x2 <- x
            y2 <- y
            if not move then
                startX2 <- x
                startY2 <- y

    ///<summary> ottiene la forza elastica dell'elastico </summary>
    member this.ForzaElastica =
        let sqr x = x * x
        let c1 = this.Module
        let c2 = single( System.Math.Sqrt( float(sqr( abs( startX1 - startX2 ) )) + float(sqr( abs( startY1 - startY2 ) )) ) )
        if c1 <= c2 then 0.f else k * abs( c1 - c2 )

    ///<summary> restituisce il modulo del vettore </summary>
    member this.Module =
        let sqr x = x * x
        single( System.Math.Sqrt( float(sqr( abs( x1 - x2 ) )) + float(sqr( abs( y1 - y2 ) )) ) )

    // controlla se l'arco da inserire e' uguale a questo
    member this.eguals( X1:float32, Y1:float32, X2:float32, Y2:float32 ) =
        x1 = X1 && x2 = X2 && y1 = Y1 && y2 = Y2 ||
        x1 = X1 && x2 = X2 && y1 = Y2 && y2 = Y1 ||
        x1 = X2 && x2 = X1 && y1 = Y1 && y2 = Y2 ||
        x1 = X2 && x2 = X1 && y1 = Y2 && y2 = Y1
        
    member this.paint( g:Graphics ) =
        g.DrawLine( Pens.Red, x1, y1, x2, y2 )
        if name <> "" then
            let sz = g.MeasureString( name, f )
            let x, y = (x1 + x2) / 2.f - sz.Width / 2.f, (y1 + y2) / 2.f - sz.Height / 2.f
            let rect = new RectangleF( new PointF( x, y ), sz )
            g.FillRectangle( Brushes.White, rect )
            g.DrawString( name, f, Brushes.Red, rect )
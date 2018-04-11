
open System.Drawing
open System.Drawing.Drawing2D

//direzioni delle freccie
type ArrowDirection =
  | UP
  | DOWN
  | LEFT
  | RIGHT

type ScrollButton( direction:ArrowDirection, area:Point[] ) =
    // path grafico per il calcolo dell'area del bottone
    let mutable gp = new GraphicsPath()
    // lista di punti raffiguranti il bottone
    let mutable points = area
    //direzione della freccia da disegnare
    let mutable dir = direction

    let clickEvt = new Event<System.EventArgs>()

    member this.Click = clickEvt.Publish

    // controlla la direzione del bottone
    member this.Direction
        with get() = dir
        and set( v ) =
            dir <- v

    // assegna/ottiene l'area del bottone
    member this.ButtonArea
        with get() = points
        and set( r ) =
            points <- r
            gp.Reset()
            gp.AddPolygon( points )

    // determina se il punto premuto e' all'interno della freccia
    member this.contains( p:Point ) =
        let ret = gp.IsVisible( p )
        if ret then
            clickEvt.Trigger( new System.EventArgs() )
        ret

    member this.paint( g:Graphics ) =
         g.FillPolygon( Brushes.Red, points )
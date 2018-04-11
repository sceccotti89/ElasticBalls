
open System.Drawing

#load "Vector.fsx"

type Physics() =
    // punto in cui si trova l'oggetto
    let mutable x, y = -1.f, -1.f
    // punto iniziale e finale dello spostamento
    let mutable start_p, end_p = new PointF(), new PointF()

    ///<summary> imposta la posizione iniziale dell'oggetto </summary>
    member this.setUpPosition( posX: float32, posY: float32 ) =
        x <- posX
        y <- posY
        end_p <- new PointF( x, y )

    // aggiunge un vettore per il calcolo della somma vettoriale
    member this.addVector( x1: float32, y1: float32 ) =
        let cx, cy = abs( x - x1 ), abs( y - y1 )
        end_p.X <- if x < x1 then end_p.X + cx else end_p.X - cx
        end_p.Y <- if y < y1 then end_p.Y + cy else end_p.Y - cy

    ///<summary> restituisce il modulo del vettore </summary>
    member this.Module =
        let sqr x = x * x
        single( System.Math.Sqrt( float(sqr( abs( x - end_p.X ) )) + float(sqr( abs( y - end_p.Y ) )) ) )

    ///<summary> restituisce il vettore </summary>
    member this.Vector =
        let v = new Vector.Vector()
        v.setPosition( start_p, end_p )
        v

    // ottiene la posizione finale del vettore
    member this.FinalPoint = end_p
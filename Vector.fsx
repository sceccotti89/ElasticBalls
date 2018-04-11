
open System.Drawing

type Vector() =
    //posizione iniziale e finale del vettore
    let mutable Start, End = new PointF(), new PointF()

    // assegna nuove posizioni
    member this.setPosition( new_start:PointF, new_end:PointF ) =
        Start <- new_start
        End <- new_end

    member this.StartPosition = Start
    member this.EndPosition = End

    ///<summary> restituisce il modulo del vettore </summary>
    member this.Module =
        let sqr x = x * x
        single( System.Math.Sqrt( float(sqr( abs( Start.X - End.X ) )) + float(sqr( abs( Start.Y - End.Y ) )) ) )
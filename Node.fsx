
open System
open System.Drawing
open System.Drawing.Drawing2D

type Node() =
    // centro del nodo
    let mutable center = new PointF()
    // dimensioni del nodo
    let mutable rect = new RectangleF()
    // area dell'immagine
    let mutable imgRect = new RectangleF()
    // dimensioni dell'immagine caricata
    let mutable imgSize = new SizeF()
    // path dell'immagine da caricare
    let mutable img_path = ""
    // velocita' di spostamento del nodo
    let mutable speed = 0.f
    // determina se il nodo e' stato selezionato
    let mutable selected = false
    // determina se e' stato selezionato per aprire il menu
    let mutable choosed = false
    // la massa del nodo
    let mutable massa = -1.f
    // vettore degli indici dei link collegati a questo nodo
    let links = new ResizeArray<int>()
    // immagine associata
    let mutable image:Bitmap = null

    ///<summary> ottiene/imposta l'area del nodo </summary>
    member this.Node
        with get() = rect
        and set( r ) =
            rect <- r
            center <- new PointF( rect.X + rect.Width / 2.f, rect.Y + rect.Height / 2.f )
            imgSize <- new SizeF( rect.Width / 2.f, rect.Height / 2.f )
            let x, y = rect.X + rect.Width / 4.f, rect.Y + rect.Height / 4.f
            imgRect <- new RectangleF( new PointF( x, y ), imgSize )
            massa <- rect.Width / 6.f

    ///<summary> ottiene/imposta il valore di selezione del nodo </summary>
    member this.Choosed
        with get() = choosed
        and set( v ) =
                choosed <- v

    ///<summary> restituisce la massa del nodo </summary>
    member this.Massa = massa

    ///<summary> ottiene/imposta il valore di selezione del nodo </summary>
    member this.Selected
        with get() = selected
        and set( v ) =
                selected <- v

    ///<summary> ottiene il vettore contenente i collegamenti del nodo </summary>
    member this.getLinks() = links

    ///<summary> aggiunge un link alla lista dei collegamenti </summary>
    member this.addLink( index:int ) =
        links.Add( index )

    ///<summary> cerca un link associato al nodo </summary>
    ///<param name = "index"> valore del link da cercare </param>
    member this.containsLink( index:int ) =
        links.Contains( index )

    ///<summary> assegna una nuova posizione al nodo modificandone il centro </summary>
    ///<param name = "x"> la coordinata X del nuovo centro </param>
    ///<param name = "y"> la coordinata Y del nuovo centro </param>
    member this.setPosition( x:float32, y:float32 ) =
        rect.X <- x - rect.Width / 2.f
        rect.Y <- y - rect.Height / 2.f
        center <- new PointF( rect.X + rect.Width / 2.f, rect.Y + rect.Height / 2.f )

    // ottiene/assegna la velocita' del nodo
    member this.Speed
        with get() = speed
        and set( v ) = 
                speed <- v

    // restituisce la forza peso del nodo
    member this.ForzaPeso =
        massa * 9.8f

    // restituisce il centro del nodo
    member this.Center = center

    // restituisce il raggio del nodo
    member this.Radius = rect.Width / 2.f

    ///<summary> assegna (o modifca) l'immagine associata </summary>
    ///<param name = "path"> il percorso per arrivare all'immagine </param>
    member this.setImage( path:string ) =
        if image <> null then
            image.Dispose()
        img_path <- path
        image <- Bitmap.FromFile( path ) :?> Bitmap
        image.SetResolution( 96.f, 96.f )

    /// rimuove l'immagine associata (se ce l'ha)
    member this.removeImage() =
        if image <> null then
            image.Dispose()
            image <- null

    ///<summary> determina se il punto e' all'interno del nodo </summary>
    ///<param name = "p"> il punto </param>
    ///<returns> TRUE se il punto e' all'interno del nodo, FALSE altrimenti </returns>
    member this.contains( p:PointF ) =
        let rad = rect.Width / 2.f
        let sqr x = x * x
        sqrt( sqr( center.X - p.X ) + sqr( center.Y - p.Y ) ) < rad

    ///<summary> disegna il nodo e l'eventuale immagine associata </summary>
    ///<param name = "g"> il contesto grafico </param>
    member this.paint( g:Graphics ) =
        if choosed then
            g.FillEllipse( Brushes.Yellow, rect )
        else
            g.FillEllipse( Brushes.White, rect )
        use p = if selected then new Pen( Color.Green, 2.f ) else new Pen( Color.Black, 2.f )
        g.DrawEllipse( p, rect )

        // disegna l'immagine associata
        if img_path <> "" then
            if image = null then 
                image <- Bitmap.FromFile( img_path ) :?> Bitmap
                image.SetResolution( 96.f, 96.f )
            let x, y = rect.X + rect.Width / 4.f, rect.Y + rect.Height / 4.f
            g.DrawImage( image, new RectangleF( new PointF( x, y ), imgSize ) )
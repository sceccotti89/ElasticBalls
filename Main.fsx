
open System.Windows.Forms
open System.Drawing
open System.Drawing.Drawing2D

#load "ToolbarButton.fsx"
#load "Node.fsx"
#load "Link.fsx"
#load "ScrollButton.fsx"
#load "Vector.fsx"
#load "Physics.fsx"
#load "Vector.fsx"
#load "AlertBox.fsx"

type ImageGraph( p:PictureBox, tb:TextBox ) as this =
    inherit UserControl()

    // posizione inizio in cui e' stato premuto il mouse
    let mutable start = new Point()
    // lista di nodi creati
    let Nodes = new ResizeArray<Node.Node>()
    // lista dei collegamenti tra nodi
    let Links = new ResizeArray<Link.Link>()
    // determina se il mouse e' stato cliccato
    let mutable pressed = false
    // nodo temporaneo disegnato
    let mutable tempNode = new Rectangle()
    // determina se si sta assegnando un nome a un link
    let mutable settingName = false
    // timer per scrollare tenendo premuto il mouse sopra una freccia
    let timerscroll = new Timer( Interval = 100 )
    // timer per gestire l'animazione
    let timerAnim = new Timer( Interval = 10 )
    // timer per la visualizzazione dei messaggi
    let timerBox = new Timer( Interval = 16 )
    // direzione premuta per scrollare
    let mutable scrolldir = ScrollButton.UP
    // angolo di rotazione del piano
    let mutable rotate = 0.f
    // determina il nodo scelto per accedere al menu
    let mutable choosed = None
    // determina il nodo selezionato
    let mutable selected = None
    // lunghezza e larghezza dei bottoni di scroll
    let btnw, btnh = 40, 40
    // valore dello zoom
    let mutable zoomscale = 1.f
    // box per la visualizzazione degli avvisi
    let alertBox = new AlertBox.AlertBox( single this.Width, single this.Height )
    // detrmina se il menu opzioni e' aperto
    let mutable isOptionOpen = false
    // l'oggetto per calcolare la fisica dei corpi
    let phy = new Physics.Physics()
    // attrito del piano
    let attritoStatico, attritoDinamico = 4.f, 3.5f
    // barra dei tasti
    let toolbar = [|
            new ToolbarButton.ToolbarButton( Type = ToolbarButton.ZOOM_MORE, Area = new Rectangle( 0, 0, btnw, btnh ) )
            new ToolbarButton.ToolbarButton( Type = ToolbarButton.ZOOM_LESS, Area = new Rectangle( btnw, 0, btnw, btnh ) )
            new ToolbarButton.ToolbarButton( Type = ToolbarButton.ROTATE_CLOCKWISE, Area = new Rectangle( 2 * btnw, 0, btnw, btnh ) )
            new ToolbarButton.ToolbarButton( Type = ToolbarButton.ROTATE_COUNTERCLOCKWISE, Area = new Rectangle( 3 * btnw, 0, btnw, btnh ) )
            new ToolbarButton.ToolbarButton( Type = ToolbarButton.ADD_LINK, Area = new Rectangle( 4 * btnw, 0, btnw, btnh ) )
            new ToolbarButton.ToolbarButton( Type = ToolbarButton.ANIMATION, Area = new Rectangle( 5 * btnw, 0, btnw, btnh ) )
      |]
    // menu opzioni di un nodo
    let options = [|
            new ToolbarButton.ToolbarButton( Type = ToolbarButton.ADD_IMAGE )
            new ToolbarButton.ToolbarButton( Type = ToolbarButton.CLOSE_MENU )
      |]
    // traslazione del piano
    let mutable translation = new PointF()
    // lista dei bottoni di scroll
    let w, h = this.Width, this.Height
    let scrollbtns = [|
        ScrollButton.ScrollButton( ScrollButton.RIGHT, [| new Point( w - btnw, (h - btnh) / 2 ); new Point( w - btnw, (h + btnh) / 2 ); new Point( w, h / 2 ) |] )
        ScrollButton.ScrollButton( ScrollButton.LEFT, [| new Point( 0, h / 2 ); new Point( btnw, (h + btnh) / 2 ); new Point( btnw, (h - btnh) / 2 ) |] )
        ScrollButton.ScrollButton( ScrollButton.DOWN, [| new Point( (w - btnw) / 2, h - btnh ); new Point( (w + btnw) / 2, h - btnh ); new Point( w / 2, h ) |] )
        ScrollButton.ScrollButton( ScrollButton.UP, [| new Point( (w - btnw) / 2, btnh ); new Point( (w + btnw) / 2, btnh ); new Point( w / 2, 0 ) |] )
      |]

    do
      this.DoubleBuffered <- true
      this.SetStyle( ControlStyles.DoubleBuffer ||| ControlStyles.AllPaintingInWmPaint, true )
      tb.Visible <- false
      // apre il file dialog
      options.[0].Click.Add( fun _ ->
            match choosed with
                | Some i ->
                    let od = new OpenFileDialog()
                    if od.ShowDialog( this ) = DialogResult.OK then
                        Nodes.[i].setImage( od.FileName )
                    Nodes.[i].Choosed <- false
                    choosed <- None
                    isOptionOpen <- false
                | _ -> ()
      )
      // chiude il menù
      options.[1].Click.Add( fun _ ->
            match choosed with
                | Some i ->
                    Nodes.[i].Choosed <- false
                    choosed <- None
                    isOptionOpen <- false
                | _ -> ()
            this.Invalidate()
      )
      // aumenta lo zoom
      toolbar.[0].Click.Add( fun _ -> zoomscale <- zoomscale * 1.1f )
      // diminuisce lo zoom
      toolbar.[1].Click.Add( fun _ -> zoomscale <- zoomscale / 1.1f )
      // routa il piano in senso orario
      toolbar.[2].Click.Add( fun _ -> rotate <- rotate + 5.f )
      // ruota il piano in senso antiorario
      toolbar.[3].Click.Add( fun _ -> rotate <- rotate - 5.f )
      // aggiunge un nuovo arco
      toolbar.[4].Click.Add( fun _ ->
            // conta prima il numero di nodi selezionati
            let mutable count = 0
            for node in Nodes do
                if node.Selected then
                    count <- count + 1
            // devono esserci solo due nodi selezionati
            if count = 2 then
                let mutable index = 0
                let mutable found, isDone = false, false
                for node in Nodes do
                    if not isDone && node.Selected then
                        let x1, y1 = node.Center.X, node.Center.Y
                        for i = 0 to Nodes.Count - 1 do
                            if index <> i && Nodes.[i].Selected then
                                let w = Nodes.[i]
                                let x2, y2 = w.Center.X, w.Center.Y
                                // controlla prima che l'arco non esista gia'
                                for link in Links do
                                    if not found && link.eguals( x1, y1, x2, y2 ) then
                                        found <- true
                                if not found then
                                    // aggiunge l'arco alla lista
                                    isDone <- true
                                    let l = new Link.Link( this.Font, x1, y1, x2, y2, index, i )
                                    Links.Add( l )
                                    Nodes.[i].addLink( Links.Count - 1 )
                                    Nodes.[index].addLink( Links.Count - 1 )
                                    settingName <- true
                                    tb.Visible <- true
                                    // assegna il focus alla textbox per inserire il nome
                                    tb.Focus() |> ignore
                                else
                                    if not alertBox.Open then
                                        alertBox.setMessage( AlertBox.LINK_ALREADY_EXISTS, single this.Width, single this.Height )
                                        timerBox.Start()
                    index <- index + 1
            else
                if not alertBox.Open then
                    alertBox.setMessage( AlertBox.WRONG_NODES, single this.Width, single this.Height )
                    timerBox.Start()
      )
      // aziona/disattiva l'animazione
      toolbar.[5].Click.Add( fun _ ->
            if timerAnim.Enabled then
                timerAnim.Stop()
            else
                timerAnim.Start()
      )

      timerAnim.Tick.Add( fun _ ->
            let mutable index = -1
            match selected with
                | Some v -> index <- v
                | _ -> ()

            // calcola e ottiene l'angolo di rotazione del vettore 
            let angle( x1: float32, y1:float32, x2:float32, y2:float32 ) =
                let cx, cy = abs( x1 - x2 ), abs( y1 - y2 )
                // ottiene l'angolo di rotazione
                let teta = atan( cy / cx )
                teta
            
            let mutable acceleration = 0.f
            // controlla gli spostamenti dei nodi
            for i = 0 to Nodes.Count - 1 do
                if i <> index then
                    let node = Nodes.[i]
                    
                    let x, y = node.Center.X, node.Center.Y
                    phy.setUpPosition( x, y )
                    
                    // forza di attrito del nodo
                    let fa = if phy.Module > 0.f then node.ForzaPeso * attritoDinamico else node.ForzaPeso * attritoStatico

                    // calcola la somma vettoriale per lo spostamento
                    let links = node.getLinks()
                    for j = 0 to links.Count - 1 do
                        let l = Links.[links.[j]]
                        let w = Nodes.[l.otherIndex( i )]
                        let fe = l.ForzaElastica
                        // calcola l'accelerazione
                        if fe - fa > 0.f then
                            acceleration <- (fe - fa) / node.Massa
                        else
                            acceleration <- 0.f
                        let x1, y1 = w.Center.X, w.Center.Y
                        let teta = angle( x, y, x1, y1 )
                        let X = if x > x1 then x - (acceleration * cos( teta )) else x + (acceleration * cos( teta ))
                        let Y = if y > y1 then y - (acceleration * sin( teta )) else y + (acceleration * sin( teta ))
                        phy.addVector( X, Y )

                    let speed = phy.Module
                                    
                    if speed > 0.f then
                        let e = phy.FinalPoint
                        let offX, offY = (e.X - x), (e.Y - y)
                        for j = 0 to links.Count - 1 do
                            Links.[links.[j]].setPosition( i, e.X, e.Y, true )
                        Nodes.[i].setPosition( e.X, e.Y )

            this.Invalidate()
      )

      timerBox.Tick.Add( fun _ ->
            alertBox.update( timerBox.Interval )
            if not alertBox.Open then
                timerBox.Stop()

            this.Invalidate()      
      )

      timerscroll.Tick.Add( fun _ ->
            // controlla la freccia premuta
            match scrolldir with
                | ScrollButton.LEFT -> translation <- new PointF( translation.X + 10.f, translation.Y )
                | ScrollButton.RIGHT -> translation <- new PointF( translation.X - 10.f, translation.Y )
                | ScrollButton.UP -> translation <- new PointF( translation.X, translation.Y + 10.f )
                | ScrollButton.DOWN -> translation <- new PointF( translation.X, translation.Y - 10.f )

            this.Invalidate()
      )

    ///<summary> apre la finestra per segnalare un avvertimento </summary>
    member this.openBox( msg:AlertBox.MESSAGE ) =
        if not alertBox.Open then
            alertBox.setMessage( msg, single this.Width, single this.Height )
            timerBox.Start()

    // assegna un nome a un arco
    member this.addName( name:string ) =
        if settingName then
            let l = Links.[Links.Count - 1]
            l.setName( name )
            settingName <- false
            this.Invalidate()
            
    // ruota un punto intorno all'origine degli assi di un angolo teta
    member this.Rotate( x:float32, y:float32, teta:float32 ) =
        // ottiene i radianti dell'angolo di rotazione espresso in sessagesimali
        let angle = teta * single System.Math.PI / 180.f
        // calcola le nuove coordinate
        let X = x * cos( angle ) - y * sin( angle )
        let Y = x * sin( angle ) + y * cos( angle )

        new PointF( X, Y )

    // trasforma le coordinate mondo in quelle vista
    member this.worldToView( x:float32, y:float32 ) =
        let mutable p = new PointF( x - translation.X, y - translation.Y )
        p <- this.Rotate( p.X, p.Y, -rotate )
        new PointF( p.X / zoomscale, p.Y / zoomscale )

    override this.OnResize e =
        base.OnResize e

        let w, h = this.Width, this.Height
        for b in scrollbtns do
          match b.Direction with
              | ScrollButton.RIGHT -> b.ButtonArea <- [| new Point( w - btnw, (h - btnh) / 2 ); new Point( w - btnw, (h + btnh) / 2 ); new Point( w, h / 2 ) |]
              | ScrollButton.LEFT -> b.ButtonArea <- [| new Point( 0, h / 2 ); new Point( btnw, (h + btnh) / 2 ); new Point( btnw, (h - btnh) / 2 ) |]
              | ScrollButton.DOWN -> b.ButtonArea <- [| new Point( (w - btnw) / 2, h - btnh ); new Point( (w + btnw) / 2, h - btnh ); new Point( w / 2, h ) |]
              | ScrollButton.UP -> b.ButtonArea <- [| new Point( (w - btnw) / 2, btnh ); new Point( (w + btnw) / 2, btnh ); new Point( w / 2, 0 ) |]
        this.Invalidate()

    override this.OnMouseDown e =
        base.OnMouseDown e

        if tb.Visible then
            tb.Focus() |> ignore
        else
            let mutable found = false
            if e.Button = MouseButtons.Left then
                for b in toolbar do
                    if not found && b.contains( e.Location ) then
                        found <- true
                        this.Invalidate()
        
            if not found && e.Button = MouseButtons.Left then
                for b in scrollbtns do
                    if not found && b.contains( e.Location ) then
                        scrolldir <- b.Direction
                        timerscroll.Start()
                        found <- true
                        this.Invalidate()

            if not found then
                if isOptionOpen then
                    if e.Button = MouseButtons.Left then
                        // controlla se ha cliccato un'opzione
                        for opt in options do
                            if not found && opt.contains( e.Location ) then
                                found <- true
                        if not found then
                            isOptionOpen <- false
                            match choosed with
                                | Some i ->
                                    Nodes.[i].Choosed <- false
                                    choosed <- None
                                | _ -> ()
                    else
                        let mutable i = Nodes.Count - 1
                        let p = this.worldToView( single e.X, single e.Y )
                        while i >= 0 && not found do
                            let w = Nodes.[i]
                            if not found && w.contains( p ) then
                                found <- true
                                match choosed with
                                    | Some index ->
                                        Nodes.[index].Choosed <- false
                                        choosed <- Some i
                                    | _ -> ()
                                w.Choosed <- true
                                // assegna l'area a partire dalla posizone del click
                                let mutable offset = 0
                                for opt in options do
                                    opt.Area <- new Rectangle( e.X + 1, e.Y + offset, btnw, btnh )
                                    offset <- offset + btnh
                            i <- i - 1
                    this.Invalidate()
                else
                    // controlla se e' stato premuto un nodo
                    if not found && Nodes.Count > 0 then
                        let mutable i = Nodes.Count - 1
                        let p = this.worldToView( single e.X, single e.Y )
                        while i >= 0 && not found do
                            let w = Nodes.[i]
                            if not found && w.contains( p ) then
                                found <- true
                                // apre il menu opzioni
                                if e.Button = MouseButtons.Right then
                                    choosed <- Some i
                                    w.Choosed <- true
                                    isOptionOpen <- true
                                    // assegna l'area a partire dalla posizone del click
                                    let mutable offset = 0
                                    for opt in options do
                                        opt.Area <- new Rectangle( e.X + 1, e.Y + offset, btnw, btnh )
                                        offset <- offset + btnh
                                else
                                    if not isOptionOpen then
                                        w.Selected <- not w.Selected
                                        selected <- Some i
                                        start <- e.Location
                                    
                                this.Invalidate()
                            i <- i - 1
                    if not found && e.Button = MouseButtons.Left then
                        pressed <- true
                        start <- e.Location

    // metodo per il calcolo della diagonale di un rettangolo
    member this.Diametro( width, heigth ) =
        single( System.Math.Sqrt( float(width * width) + float(heigth * heigth) ) )

    override this.OnMouseMove e =
        base.OnMouseMove e

        if timerscroll.Enabled then
            let mutable found = false
            for s in scrollbtns do
                if not found && s.contains( e.Location ) then
                    found <- true
            if not found then
                timerscroll.Stop()

        // controlla gli spostamenti dei nodi
        match selected with
            | Some v ->
                let n = Nodes.[v]

                // calcola lo spostamento
                let p = this.Rotate( single( e.X - start.X ), single( e.Y - start.Y ), -rotate )
                let offset = new PointF( p.X / zoomscale, p.Y / zoomscale )

                let x, y = n.Center.X, n.Center.Y
                // aggiorna il nodo
                let node = n.Node
                Nodes.[v].setPosition( x + offset.X, y + offset.Y )
                    
                // aggiorna i vari link collegati al nodo
                let c = Nodes.[v].Center
                let links = Nodes.[v].getLinks()
                let move = timerAnim.Enabled
                for i in links do
                    Links.[i].setPosition( v, c.X, c.Y, move )

                start <- e.Location
                this.Invalidate()
            | _ -> ()

        if pressed then
            let diametro = int( this.Diametro( e.X - start.X, e.Y - start.Y ) )
            tempNode <- new Rectangle( start.X - diametro, start.Y - diametro, diametro * 2, diametro * 2 )
            this.Invalidate()

    override this.OnMouseUp e =
        base.OnMouseUp e

        if timerscroll.Enabled then
            timerscroll.Stop()

        // rilascia il nodo se ne e' stato selezionato uno
        match selected with
            | Some v ->
                selected <- None
            | _ -> ()

        if pressed then
            // aggiunge un nuovo nodo solo se il mouse si e' spostato dal punto iniziale
            if start.X <> e.X || start.Y <> e.Y then
                let diametro = this.Diametro( e.X - start.X, e.Y - start.Y )
                let size = (diametro * 2.f) / zoomscale

                let mutable p = new PointF( single start.X - translation.X, single start.Y - translation.Y )
                p <- this.Rotate( p.X, p.Y, -rotate )
                let x, y = (p.X - diametro) / zoomscale, (p.Y - diametro) / zoomscale

                let n = new Node.Node( Node = new RectangleF( x, y, size, size ) )
                Nodes.Add( n )
                let v = new Vector.Vector()
                let c = n.Center
                v.setPosition( new PointF( c.X, c.Y ), new PointF( c.X, c.Y ) )
            
            pressed <- false
            this.Invalidate()
        else
            let mutable found = false
            if e.Button = MouseButtons.Left then
                for t in toolbar do
                    if not found && (t.contains( e.Location ) || t.Pressed) then
                        t.Pressed <- false
                        found <- true
                        this.Invalidate()

                if not found then
                    for opt in options do
                        if not found && (opt.contains( e.Location ) || opt.Pressed) then
                            opt.Pressed <- false
                            found <- true
                            this.Invalidate()

    override this.OnKeyDown e =
        base.OnKeyDown e

        match e.KeyCode with
          | Keys.W -> translation <- new PointF( translation.X, translation.Y + 10.f )
          | Keys.A -> translation <- new PointF( translation.X + 10.f, translation.Y )
          | Keys.S -> translation <- new PointF( translation.X, translation.Y - 10.f )
          | Keys.D -> translation <- new PointF( translation.X - 10.f, translation.Y )
          | Keys.R -> rotate <- rotate - 5.f
          | Keys.U -> rotate <- rotate + 5.f
          | Keys.Q -> zoomscale <- zoomscale * 1.1f
          | Keys.E -> zoomscale <- zoomscale / 1.1f
          | _ -> ()

        this.Invalidate()

    override this.OnPaint e =
        base.OnPaint e

        let g = e.Graphics
        g.SmoothingMode <- Drawing2D.SmoothingMode.HighQuality
        g.SmoothingMode <- Drawing2D.SmoothingMode.AntiAlias

        // trasla, ruota e scala il piano
        g.TranslateTransform( translation.X, translation.Y )
        g.RotateTransform( rotate )
        g.ScaleTransform( zoomscale, zoomscale )

        // calcola l'area del controllo passando da coordinate mondo a quelle vista
        let r = this.ClientRectangle
        let a = this.worldToView( 0.f, 0.f )
        let b = this.worldToView( single r.Width, 0.f )
        let c = this.worldToView( 0.f,single r.Height )

        // esegue la somma vettoriale per calcolare il punto in basso a destra
        let cx, cy = abs( c.X - a.X ), abs( c.Y - a.Y )
        let mutable end_x = if a.X < c.X then a.X + cx else a.X - cx
        let mutable end_y = if a.Y < c.Y then a.Y + cy else a.Y - cy
        let cx, cy = abs( b.X - a.X ), abs( b.Y - a.Y )
        end_x <- if a.X < b.X then end_x + cx else end_x - cx
        end_y <- if a.Y < b.Y then end_y + cy else end_y - cy

        let box = [| a; c; new PointF( end_x, end_y ); b |]
        use gp = new GraphicsPath()
        gp.AddPolygon( box )
        use region = new Region( gp )
        
        // inserisce i collegamenti
        for link in Links do
            link.paint( g )
        
        // inserisce i nodi
        for node in Nodes do
            if region.IsVisible( node.Node ) then
                node.paint( g )
            else
                node.removeImage()

        // ripristina le coordinate originali
        g.ResetTransform()

        // disegna il nodo temporaneo
        if pressed then
            g.FillEllipse( Brushes.White, tempNode )
            g.DrawEllipse( Pens.Black, tempNode )

        let back = this.BackColor
        for b in toolbar do
            b.paint( g, back )

        for b in scrollbtns do
            b.paint( g )

        if isOptionOpen then
            for opt in options do
                opt.paint( g, back )

        alertBox.paint( this.Font, g )

let f = new Form( Text = "Image Graph", TopMost = true, Width = 900, Height = 600 )

// caricatore di immagini
let p = new PictureBox( Dock = DockStyle.Fill )

// la textBox
let tb = new TextBox( Dock = DockStyle.Bottom )
let imageGraph = new ImageGraph( p, tb, Dock = DockStyle.Fill )

tb.KeyDown.Add( fun e ->
    match e.KeyData with
        | Keys.Enter ->
            let text = tb.Text
            let mutable found = false
            for i = 0 to text.Length - 1 do
                if not found && text.Chars( i ) <> ' ' then
                    found <- true
                    tb.Text <- text.Substring( i )
            if found then
                imageGraph.addName( tb.Text )
                tb.Text <- ""
                tb.Visible <- false
                imageGraph.Focus() |> ignore
            else
                if text.Length = 0 then
                    imageGraph.openBox( AlertBox.MSG_TEXT_TOO_SHORT )
                else
                    imageGraph.openBox( AlertBox.MSG_BAD_FORMAT )
        | _ -> ()
)

f.Controls.Add( imageGraph )
f.Controls.Add( p )
f.Controls.Add( tb )

f.Show()
imageGraph.Focus() |> ignore
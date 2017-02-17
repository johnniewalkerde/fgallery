#if BOOTSTRAP
System.Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
if not (System.IO.File.Exists "paket.exe") then let url = "https://github.com/fsprojects/Paket/releases/download/3.13.3/paket.exe" in use wc = new System.Net.WebClient() in let tmp = System.IO.Path.GetTempFileName() in wc.DownloadFile(url, tmp); System.IO.File.Move(tmp,System.IO.Path.GetFileName url);;
#r "paket.exe"
Paket.Dependencies.Install (System.IO.File.ReadAllText "paket.dependencies")
#endif

//---------------------------------------------------------------------

#I "packages/Suave/lib/net40"
#r "packages/Suave/lib/net40/Suave.dll"

open System
open Suave                 // always open suave
open Suave.Http
open Suave.Filters
open Suave.Successful // for OK-result
open Suave.Web             // for config
open System.Net
open Suave.Operators 

printfn "initializing script..."

let config = 
    let port = 
      let p = box (System.Environment.GetEnvironmentVariable("PORT")) 
      if p = null then
        None
      else
        let port = unbox p
        Some(port |> string |> int)
    let ip127  = "127.0.0.1"
    let ipZero = "0.0.0.0"

    { defaultConfig with 
        //logger = Logging.Loggers.saneDefaultsFor Logging.LogLevel.Verbose
        bindings=[ (match port with 
                     | None -> HttpBinding.createSimple HTTP ip127 8080
                     | Some p -> HttpBinding.createSimple HTTP ipZero p) ] }

let angularHeader = """<head>
<link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/bootstrap/3.2.0/css/bootstrap.min.css">
<script src="https://ajax.googleapis.com/ajax/libs/angularjs/1.2.26/angular.min.js"></script>
</head>"""

let thingsText n = 
    [ yield """<html>"""
      yield angularHeader
      yield """ <body>"""
      yield """ <h1>Endangered Animals</h1>"""
      yield """  <table class="table table-striped">"""
      yield """   <thead><tr><th>Thing</th><th>Value</th></tr></thead>"""
      yield """   <tbody>"""
      for i in 1 .. n do
         yield sprintf "<tr><td>Thing %d</td><td>%d</td></tr>" i i  
      yield """   </tbody>"""
      yield """  </table>"""
      yield """ </body>""" 
      yield """</html>""" ]
    |> String.concat "\n"

let homePage = 
    [ yield """<html>"""
      yield angularHeader 
      yield """ <body>"""
      yield """ <h1>Sample Web App</h1>"""
      yield """  <table class="table table-striped">"""
      yield """   <thead><tr><th>Page</th><th>Link</th></tr></thead>"""
      yield """   <tbody>"""
      yield """      <tr><td>Endangered Animals</td><td><a href="/animals">Link to animals</a></td></tr>""" 
      yield """      <tr><td>Things</td><td><a href="/things/10">Link to things (10)</a></td></tr>""" 
      yield """      <tr><td>Things</td><td><a href="/things/100">Link to things (100)</a></td></tr>""" 
      yield """      <tr><td>API JSON</td><td><a href="/api/json/100">Link to result (100)</a></td></tr>"""
      yield """      <tr><td>API XML</td><td><a href="/api/xml/100">Link to result (100)</a></td></tr>"""
      yield """      <tr><td>API JSON</td><td><a href="/api/json/10">Link to result (10)</a></td></tr>"""
      yield """      <tr><td>API XML</td><td><a href="/api/xml/10">Link to result (10)</a></td></tr>"""
      yield """   </tbody>"""
      yield """  </table>"""
      yield """ </body>""" 
      yield """</html>""" ]
    |> String.concat "\n"

printfn "starting web server..."

let jsonText n = 
    """
{"menu": {
  "id": "file",
  "value": "File",
  "popup": {
    "result": [
""" + String.concat "\n"
      [ for i in 1 .. n -> sprintf """{"value": "%d"},""" i ] + """
    ]
  }
}}""" 

let xmlText n = 
    """
<menu id="file" value="File">
  <popup>
""" + String.concat "\n"
      [ for i in 1 .. n -> sprintf """<menuitem value="%d" />""" i ] + """
    <menuitem value="Open" />
    <menuitem value="Close"  />
  </popup>
</menu>""" 

let xmlMime = Writers.setMimeType "application/xml"
let jsonMime = Writers.setMimeType "application/json"
let app = 
  choose
    [ GET >=> choose
                [ path "/" >=> OK homePage
                  pathScan "/things/%d" (fun n -> OK (thingsText n))
                  path "/api/json" >=> jsonMime >=> OK (jsonText 100)
                  pathScan "/api/json/%d" (fun n -> jsonMime >=> OK (jsonText n))
                  path "/api/xml" >=> xmlMime >=> OK (xmlText 100)
                  pathScan "/api/xml/%d" (fun n -> xmlMime >=> OK (xmlText n)) ]
      POST >=> choose
                [ path "/hello" >=> OK "Hello POST"
                  path "/goodbye" >=> OK "Good bye POST" ] ]
    
#if INTERACTIVE
#else
startWebServer config app
printfn "exiting server..."
#endif



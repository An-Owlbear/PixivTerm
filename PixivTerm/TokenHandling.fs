namespace PixivTerm
open System
open System.IO

module TokenHandling =
    // Writes the given parameter to a file
    
    let mutable tokens = String.Empty
    let mutable ID = 0
    
    let writeFile (input: Stream) filename =
        let writer = File.Create(filename)
        input.CopyTo(writer)
        writer.Close()
        filename

    // functions to store/read tokens
    let storeTokens (tokens : string list) =
        let writeString = String.Join(",", tokens @ [string ID])
        
        use sw = new StreamWriter("tokens.txt", false)
        sw.Write writeString
        
    let readTokens () =
         if not (File.Exists("tokens.txt")) then
             let file = File.Create("tokens.txt")
             file.Close()
         
         try
             let fileString = File.ReadAllText("tokens.txt")
             let fileArray = fileString.Split ","
             ID <- fileArray.[fileArray.Length - 1] |> int
             tokens <- String.Join(",", ([0..2] |> List.map (fun x -> fileArray.[x])))
             true
         with
         | _ ->
             File.WriteAllText("tokens.txt", String.Empty)
             false
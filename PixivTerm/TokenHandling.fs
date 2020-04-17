namespace PixivTerm
open System
open System.IO

module TokenHandling =
    // Writes the given parameter to a file
    
    let mutable tokens = String.Empty
    
    let writeFile (input: Stream) filename =
        let writer = File.Create(filename)
        input.CopyTo(writer)
        writer.Close()
        filename

    // functions to store/read tokens
    let storeTokens (tokens : string list) =
        let writeString = String.Join(",", tokens)
        File.WriteAllText("tokens.txt", writeString)
        
    let readTokens () =
         tokens <- File.ReadAllText("tokens.txt")
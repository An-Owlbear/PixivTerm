namespace PixivTerm
open System.Diagnostics
open System.IO
open PixivCSharp

module Requests =
    let client = PixivClient()
    let mutable account = LoginResponse()

    // Runs a task synchronously
    let sendRequest task =
        task |> Async.AwaitTask |> Async.RunSynchronously
        
    // Recieves input from the user to login
    let loginRequest username password =
        client.LoginAsync(username, password) |> sendRequest
        
    let refreshLogin access refresh device =
        client.SetTokens(access, refresh, device)
        client.RefreshLoginAsync() |> sendRequest
        
    // Gets a search result for popular illusts
    let searchPopular target =
        client.PopularIllustsPreviewAsync(target) |> sendRequest |> fun x -> List.ofSeq x.Illusts
        
    let recommended () =
        client.RecommendedIllustsAsync() |> sendRequest |> fun x -> List.ofSeq x.Illusts
        
    // Methods for viewing and download images
    let viewIllust id =
        client.ViewIllustAsync(id) |> sendRequest
        
    let writeStream (stream : Stream) filename =
        let filestream = File.Create("/tmp/" + filename)
        stream.CopyTo(filestream)
        filestream.Close()
    
    let viewSingleImage (url : string) =
        let filename = url.Split "/" |> fun x -> x.[x.Length - 1]
        let image = client.GetImageAsync(url) |> sendRequest
        writeStream image filename
        let eog = Process.Start("eog", "/tmp/" + filename)
        eog.WaitForExit()
        File.Delete("/tmp/" + filename)
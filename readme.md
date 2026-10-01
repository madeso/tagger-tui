# Tagger (tui/cli)

Tagger is a simple tool to add tags to files and then do things with thoose tags (like move and show statistics)

It is heavily inspired by the music player [foobar2k](https://www.foobar2000.org/).

## Sample usage
I initialize the store and add some files
```powershell
tagger init
tagger add content/posts/*.md
```

Then I launch the TUI and edit the properties. Using the extractor I can get set the name property to the filenhame using a simple `%name%` pattern or setting the name property to the foldername using the `%name%/index`.
```powershell
tagger edit
```

When I'm satisfied with the properties I can move all files at once using a pattern.
```powershell
tagger move "content/posts/`$replace(%name%,-,_)/index" # inspect what the move will do
tagger move "content/posts/`$replace(%name%,-,_)/index" -r # run the move
```
Here `%foo%` is using the property from a file and `$replace(...)` is replacing all `-` with `_`.

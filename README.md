# ModLib
A simple C# utility for parsing Fabric Minecraft mods.

# Usage
    var path = "path\\to\\jar\\file.jar"
    var modMeta = new Parser.ModMeta().FromModFile(path);
    
    var name = modMeta.Name
    var authors = modMeta.Authors
    etc..

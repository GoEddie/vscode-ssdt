# vscode-ssdt
do some ssdt stuff like build a dacpac from vscode 

If you want to run this, open the "SSDTWrap" solution in visual studio ctrl+f5 to run it then open vs code and open the folder src\s2 and open the file src\s2\src\extension.ts and f5 - this should open a new vs code window.

Open a folder with the .sql files(it searches sub directories etc) and add a ssdt.json file to the root which looks like:

{
    "outputFile": "c:\\dev\\abc.dacpac",
    "SqlServerVersion": "Sql130",
    "references":[
        {
            "type": "same",
            "path": "C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\Common7\\IDE\\Extensions\\Microsoft\\SQLDB\\Extensions\\SqlServer\\140\\SQLSchemas\\master.dacpac"
        }
    ],
    "PreScript": "",
    "PostScript": "",
    "RefactorLog": "",
    "ignoreFilter": "*script*",
    "PublishProfilePath": "C:\\dev\\SSDT-DevPack\\src\\Test\\Common\\SampleSolutions\\NestedProjects\\Nested2\\Nested2.publish.xml"
}



this is only a hacky start, any non-build scripts will be added to the model and pre/post/refactorlog/references aren't supported yet so just for fun really!

For the motivation for this see:

https://the.agilesql.club/blogs/Ed-Elliott/2017-04-27/Building-SSDT-Projects-In-Visual-Studio-Code-A-Hacky-Experiment




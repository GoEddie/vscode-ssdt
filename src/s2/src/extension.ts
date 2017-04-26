'use strict';
/// <reference path="context.ts"/>

//sort of have filling the model on load, need to return messages and also add / change files as they are modified...


import * as vscode from 'vscode';
import * as http from 'http';
import * as path from 'path';

let fs = require('fs');

var g_s2Context: s2Context;
var g_ErrorsChannel: vscode.OutputChannel;

function showErrors(context: s2Context){
    g_ErrorsChannel.clear();
    for(var message of context.Messages){
        console.log(message.Message);
        g_ErrorsChannel.appendLine(message.Prefix + message.ErrorCode + ": " + message.Message + " " +message.SourceName.replace(':0', '') + " line " + message.Line + " column: " + message.Column);        
    }   

    g_ErrorsChannel.show(); 
}

function setContext(context: s2Context){
    g_s2Context = context;
    showErrors(context);
    console.log('we have a new context, token: ' + g_s2Context.Token);
}

function register(dir: string) {

    let data = {
        'Directory': dir,
        'JsonConfig': path.join(dir, 'ssdt.json')
    };
    
    let client = http.createClient(14801, "127.0.0.1");
    let headers = {
        'Host': '127.0.0.1',
        'Content-Type': 'application/json'
    };

    let request = client.request('POST', '/register/', headers);
    var context: s2Context;
    request.on('response', function (response) {
        var json = ''
        response.on('data', function (chunk) {            
            //setContext(<s2Context> JSON.parse(chunk));
            json += chunk;
        });
        response.on('end', function () {
            console.log('response ended');
            setContext(<s2Context> JSON.parse(json));

                var watcher = vscode.workspace.createFileSystemWatcher("**/*.sql"); //glob search string
             //   watcher.ignoreChangeEvents = false;

                watcher.onDidChange(() => {
                    vscode.window.showInformationMessage("change applied!"); //In my opinion this should be called
                    
                    updateFile(vscode.window.activeTextEditor.document.fileName);

                });

        });
    });
    
    request.write(JSON.stringify(data));

    request.end();
}


function updateFile(fileName: string) {

    if(g_s2Context == null){
        return;
    }
    
    let data = {
        'Token': g_s2Context.Token,
        'File': fileName        
    };
    
    let client = http.createClient(14801, "127.0.0.1");
    let headers = {
        'Host': '127.0.0.1',
        'Content-Type': 'application/json'
    };

    let request = client.request('POST', '/update/', headers);
    var context: s2Context;
    request.on('response', function (response) {
        var json = '';
        response.on('data', function (chunk) {            
            // setContext(<s2Context> JSON.parse(chunk));
            // console.log(JSON.parse(chunk));
            //console.log(JSON.parse(chunk));
            json += chunk;
            
        });
        response.on('end', function () {
            console.log('response ended with code: ' + response.statusCode);

            showErrors(<s2Context> JSON.parse(json));
        });
    });
    
    request.write(JSON.stringify(data));

    request.end();
}


function findReferences(schema: string, name: string) {
//NEXT Need to include token - not sure how to store global variables? or what right approach is, vscode context??
    if(g_s2Context == null){
        return;
    }
    
    let data = {
        'Token': g_s2Context.Token,
        'SchemaName': schema,
        'Name': name
    };
    
    let client = http.createClient(14801, "127.0.0.1");
    let headers = {
        'Host': '127.0.0.1',
        'Content-Type': 'application/json'
    };

    let request = client.request('POST', '/references/', headers);
    var context: s2Context;
    request.on('response', function (response) {
        response.on('data', function (chunk) {            
            // setContext(<s2Context> JSON.parse(chunk));
            // console.log(JSON.parse(chunk));
            console.log(JSON.parse(chunk));
            
        });
        response.on('end', function () {
            console.log('response ended with code: ' + response.statusCode);
        });
    });
    
    request.write(JSON.stringify(data));

    request.end();
}

function buildDacpac() {
//NEXT Need to include token - not sure how to store global variables? or what right approach is, vscode context??
    if(g_s2Context == null){
        return;
    }
    
    let data = {
        'Token': g_s2Context.Token        
    };
    
    let client = http.createClient(14801, "127.0.0.1");
    let headers = {
        'Host': '127.0.0.1',
        'Content-Type': 'application/json'
    };

    let request = client.request('POST', '/build/', headers);
    var context: s2Context;
    request.on('response', function (response) {
        var json: string = '';
        response.on('data', function (chunk) {            
            // setContext(<s2Context> JSON.parse(chunk));
            // console.log(JSON.parse(chunk));
            //console.log(JSON.parse(chunk));
            json += chunk;
            
        });
        response.on('end', function () {
            console.log('response ended with code: ' + response.statusCode);
            console.log(JSON.parse(json));
        });
    });
    
    request.write(JSON.stringify(data));

    request.end();
}

export function referenceProvider(selector: vscode.DocumentSelector, provider: vscode.ReferenceProvider){
    
}

// this method is called when your extension is activated
// your extension is activated the very first time the command is executed
export function activate(context: vscode.ExtensionContext) {
    
    register(vscode.workspace.rootPath);
    // vscode.languages.registerReferenceProvider(function(selector: vscode.DocumentSelector, provider: vscode.ReferenceProvider){

    // });
    let disposable = vscode.commands.registerCommand('extension.buildDacpac', () => {
        //        vscode.window.showInformationMessage('Built!');
        buildDacpac();
    });
    
    g_ErrorsChannel = vscode.window.createOutputChannel("SSDT Validation Errors");

    const providerRegistrations = vscode.Disposable.from(		
		vscode.languages.registerReferenceProvider('sql', new ssdtReferenceProvider())
	);

    context.subscriptions.push(disposable);
}

// this method is called when your extension is deactivated
export function deactivate() {
    console.log('deactivated');
}

export  class ssdtReferenceProvider implements vscode.ReferenceProvider {
    provideReferences(document: vscode.TextDocument, position: vscode.Position, context: vscode.ReferenceContext, token: vscode.CancellationToken): vscode.ProviderResult<vscode.Location[]> {
        
        let finder = new NameFinder();
        let name: SqlName = finder.findName(document, position);

        console.log('referenceProvider, schema: ' + name.Schema + ' name: ' + name.Name + ' - ' + g_s2Context.Token);

        findReferences(name.Schema, name.Name);

        var place: vscode.Location = new vscode.Location(vscode.Uri.parse('file:///c%3A/dev/SSDT-DevPack/src/Test/Common/SampleSolutions/NestedProjects/1.sql'), new vscode.Position(3, 4));
        var places: vscode.Location[];
        places.push(place);

        

        return places; 

    }
}

class NameFinder {
    findName(document: vscode.TextDocument, position: vscode.Position) : SqlName{
        var text = document.getText().split('\n');
        var range = document.getWordRangeAtPosition(position);
        var selected: string = text[range.start.line].substring(range.start.character*2, range.end.character*2 - range.start.character*2);
        var temp: string = '';

        var lastChar  = '';
        for(var char of selected){
            if(char == '-' && lastChar == '-'){
                break;
            }
            
            if(char == ';')
            {
              break;   
            }
            
            if(char == '*' && lastChar == '/'){
                break;
            }

            if(char != '-' && char != '/'){
                temp += char;
            }
            
            lastChar = char;
        }

        return this.nameToSqlName(temp.trim());
        
    }

    nameToSqlName(name: string) : SqlName{
        
        var parts: string[] = name.split('.');
        if(parts.length == 1){
            return new SqlName(null, null, null, name);
        }

        if(parts.length == 2){
            return new SqlName(null, null, parts[0], parts[1]);
        }
        
        if(parts.length == 3){
            return new SqlName(null, parts[0], parts[1], parts[2]);
        }
            
       return new SqlName(parts[0], parts[1], parts[2], parts[3]);
  }
}

class SqlName {
    Server: string;
    Database: string;
    Schema: string;
    Name: string;
   
    constructor(server: string, database: string, schema: string, name: string) {
        this.Server = server;
        this.Database = database;
        this.Schema = schema;
        this.Name = name;
    }
}
using ConsoleTables;
using Microsoft.VisualBasic.FileIO;

namespace Compiler;

using System;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        double x = .0e5;
        string? filePath = "/home/rinka-admin/RiderProjects/Compiler/Compiler/Example.txt";

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Файл {filePath} не найден!");
            return;
        }

        string code = File.ReadAllText(filePath);
        
        Lexer lexer = new Lexer(code);

        List<Token> tokens = lexer.Tokenize();

        var table = new ConsoleTable("Line", "Column", "Type", "Value");
        foreach (var token in tokens)
        {
            table.AddRow(token.Line, token.Column, token.Type, token.Value.Trim());
        }
        table.Write();
        Console.WriteLine();
    }
}
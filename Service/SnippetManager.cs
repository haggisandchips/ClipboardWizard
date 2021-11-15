using ClipboardWizard.Model;
using SQLite;
using System;
using System.Collections.Generic;

namespace ClipboardWizard.Service
{
    public class SnippetManager
    {
        public static void SaveSnippet(Snippet snippet)
        {
            using SQLiteConnection connection = new(App.databasePath);
            _ = connection.CreateTable<Snippet>();
            _ = connection.Insert(snippet);
        }

        public static void UpdateSnippet(Snippet snippet)
        {
            using SQLiteConnection connection = new(App.databasePath);
            _ = connection.CreateTable<Snippet>();
            _ = connection.Update(snippet);
        }

        public static List<Snippet> LoadSnippets()
        {
            using SQLiteConnection connection = new(App.databasePath);
            _ = connection.CreateTable<Snippet>();

            return connection.Table<Snippet>()
                .OrderBy(Snippet => Snippet.Order)
                .ToList();
        }

        internal static void DeleteSnippet(Snippet snippet)
        {
            using SQLiteConnection connection = new(App.databasePath);
            _ = connection.Delete(snippet);
        }
    }
}

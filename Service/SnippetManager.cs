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

        internal static void OrderHigher(Snippet snippet)
        {
            Reorder(snippet, -1);
        }

        internal static void OrderLower(Snippet snippet)
        {
            Reorder(snippet, +1);
        }

        private static void Reorder(Snippet snippet, int indexOffset)
        {
            using SQLiteConnection connection = new(App.databasePath);
            _ = connection.CreateTable<Snippet>();

            List<Snippet> snippets = connection.Table<Snippet>()
                .OrderBy(Snippet => Snippet.Order)
                .ToList();

            int currentIndex = snippets.FindIndex(s => s.Id == snippet.Id);
            Snippet currentSnippet = snippets[currentIndex];
            Snippet otherSnippet = snippets[currentIndex + indexOffset];
            (currentSnippet.Order, otherSnippet.Order) = (otherSnippet.Order, currentSnippet.Order);

            _ = connection.Update(currentSnippet);
            _ = connection.Update(otherSnippet);
        }
    }
}

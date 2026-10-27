using System;
using System.Collections.Generic;
using System.Text;

namespace Commons.Addressables.Editor
{
    /// <summary>A static class in the generated keys tree: nested classes plus <c>const string</c> keys.</summary>
    internal sealed class KeyNode
    {
        private const string INDENT = "    ";
        private const string UNIQUE_SUFFIX_FORMAT = "{0}_{1}";
        private const int FIRST_DUPLICATE_SUFFIX = 2;

        private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const",
            "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
            "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface",
            "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override",
            "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof",
            "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
        };

        private readonly SortedDictionary<string, KeyNode> _children = new(StringComparer.Ordinal);
        private readonly SortedDictionary<string, string> _constants = new(StringComparer.Ordinal);

        public KeyNode(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public KeyNode Child(string name)
        {
            if (!_children.TryGetValue(name, out var child))
            {
                child = new KeyNode(name);
                _children[name] = child;
            }

            return child;
        }

        public void AddConstant(string name, string value)
            => _constants[name] = value;

        public void Write(StringBuilder builder, int depth)
            => Write(builder, depth, Sanitize(Name));

        private void Write(StringBuilder builder, int depth, string identifier)
        {
            var indent = Indent(depth);
            builder.Append(indent).Append("public static class ").AppendLine(identifier);
            builder.Append(indent).AppendLine("{");

            var used = new HashSet<string>(StringComparer.Ordinal) { identifier };

            foreach (var constant in _constants)
            {
                builder.Append(indent).Append(INDENT)
                    .Append("public const string ").Append(Unique(Sanitize(constant.Key), used))
                    .Append(" = \"").Append(Escape(constant.Value)).AppendLine("\";");
            }

            var first = _constants.Count == 0;
            foreach (var child in _children.Values)
            {
                if (!first)
                    builder.AppendLine();

                first = false;
                child.Write(builder, depth + 1, Unique(Sanitize(child.Name), used));
            }

            builder.Append(indent).AppendLine("}");
        }

        private static string Unique(string identifier, HashSet<string> used)
        {
            var candidate = identifier;
            for (var suffix = FIRST_DUPLICATE_SUFFIX; !used.Add(candidate); suffix++)
                candidate = string.Format(UNIQUE_SUFFIX_FORMAT, identifier, suffix);

            return candidate;
        }

        private static string Sanitize(string name)
        {
            var builder = new StringBuilder(name.Length + 1);
            foreach (var character in name)
                builder.Append(char.IsLetterOrDigit(character) || character == '_' ? character : '_');

            if (builder.Length == 0 || char.IsDigit(builder[0]))
                builder.Insert(0, '_');

            var identifier = builder.ToString();
            return CSharpKeywords.Contains(identifier) ? "@" + identifier : identifier;
        }

        private static string Escape(string value)
            => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

        private static string Indent(int depth)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < depth; i++)
                builder.Append(INDENT);
            return builder.ToString();
        }
    }
}

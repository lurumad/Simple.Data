namespace Simple.Data.Ado
{
    using System;
    using Schema;

    class SimpleReferenceFormatter
    {
        private readonly IFunctionNameConverter _functionNameConverter = new FunctionNameConverter();
        private readonly DatabaseSchema _schema;

        public SimpleReferenceFormatter(DatabaseSchema schema)
        {
            _schema = schema;
        }

        public string FormatColumnClause(SimpleReference reference)
        {
            var formatted = TryFormatAsObjectReference(reference as ObjectReference)
                            ??
                            TryFormatAsFunctionReference(reference as FunctionReference)
                            ??
                            TryFormatAsMathReference(reference as MathReference);

            if (formatted != null) return formatted;

            throw new InvalidOperationException("SimpleReference type not supported.");
        }

        private string FormatObject(object obj)
        {
            var reference = obj as SimpleReference;
            return reference != null ? FormatColumnClause(reference) : obj.ToString();
        }

        private string TryFormatAsMathReference(MathReference mathReference)
        {
            if (ReferenceEquals(mathReference, null)) return null;

            return string.Format("{0} {1} {2}", FormatObject(mathReference.LeftOperand),
                                 MathOperatorToString(mathReference.Operator), FormatObject(mathReference.RightOperand));
        }

        private static string MathOperatorToString(MathOperator @operator)
        {
            switch (@operator)
            {
                case MathOperator.Add:
                    return "+";
                case MathOperator.Subtract:
                    return "-";
                case MathOperator.Multiply:
                    return "*";
                case MathOperator.Divide:
                    return "/";
                case MathOperator.Modulo:
                    return "%";
                default:
                    throw new InvalidOperationException("Invalid MathOperator specified.");
            }
        }

        private string TryFormatAsFunctionReference(FunctionReference functionReference)
        {
            if (ReferenceEquals(functionReference, null)) return null;

            var sqlName = _functionNameConverter.ConvertToSqlName(functionReference.Name);
            return functionReference.Alias == null
                       ? string.Format("{0}({1})", sqlName,
                                       FormatColumnClause(functionReference.Argument))
                       : string.Format("{0}({1}) AS {2}", sqlName,
                                       FormatColumnClause(functionReference.Argument),
                                       _schema.QuoteObjectName(functionReference.Alias));
        }

        private string TryFormatAsObjectReference(ObjectReference objectReference)
        {
            if (ReferenceEquals(objectReference, null)) return null;

            var table = _schema.FindTable(objectReference.GetOwner().GetAllObjectNamesDotted());
            var tableName = string.IsNullOrWhiteSpace(objectReference.GetOwner().Alias)
                                ? table.QualifiedName
                                : _schema.QuoteObjectName(objectReference.GetOwner().Alias);
            var column = table.FindColumn(objectReference.GetName());
            if (objectReference.Alias == null)
                return string.Format("{0}.{1}", tableName, column.QuotedName);
            else
                return string.Format("{0}.{1} AS {2}", tableName, column.QuotedName,
                                     _schema.QuoteObjectName(objectReference.Alias));
        }

    }
}
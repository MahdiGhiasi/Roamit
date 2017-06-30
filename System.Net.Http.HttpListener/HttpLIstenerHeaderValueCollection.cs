using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace System.Net.Http
{
    public sealed class HttpListenerHeaderValueCollection<T> : Collection<T>
    {
        internal HttpListenerHeaderValueCollection(HttpListenerHeaders headers, string headerName)
        {
            Headers = headers;
            HeaderName = headerName;

            string valuesString = null;

            if (Headers.TryGetValue(HeaderName, out valuesString))
            {
                if (valuesString == null)
                    return;

                var isString = typeof(T) == typeof(string);

                if (isString)
                {
                    foreach (var value in valuesString.Split(','))
                    {
                        Add((T)(object)value.Trim());
                    }
                }
                else
                {
                    var parseMethod = typeof(T).GetTypeInfo().GetDeclaredMethod("Parse");

                    if (parseMethod == null)
                        throw new NotSupportedException("Type is not supported.");

                    foreach (var value in valuesString.Split(','))
                    {
                        Add((T)parseMethod.Invoke(value.Trim(), new object[0]));
                    }
                }
            }
        }

        private string HeaderName { get; }
        private HttpListenerHeaders Headers { get; }

        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);

            Headers[HeaderName] = ToString();
        }

        protected override void ClearItems()
        {
            base.ClearItems();

            Headers[HeaderName] = ToString();
        }

        protected override void RemoveItem(int index)
        {
            base.RemoveItem(index);

            Headers[HeaderName] = ToString();
        }

        protected override void SetItem(int index, T item)
        {
            base.SetItem(index, item);


            Headers[HeaderName] = ToString();
        }
        public override string ToString()
        {
            var sb = new StringBuilder();
            var items = this.ToArray();
            foreach (var value in items)
            {
                sb.Append(value);
                if (!value.Equals(this.Last()))
                {
                    sb.Append(", ");
                }
            }
            return sb.ToString();
        }
    }
}

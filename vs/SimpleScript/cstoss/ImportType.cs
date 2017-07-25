using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleScript
{
    public interface IImportTypeHandler
    {
        object GetValueFromCSToSS(object obj, object key);
        void SetValueFromSSToCS(object obj, object key, object value);
    }

    public class DelegateGenerateManager
    {
        Dictionary<Type, Func<Closure, Delegate>> _generaters = new Dictionary<Type, Func<Closure, Delegate>>();
        public void RegisterGenerater(Type t, Func<Closure, Delegate> generater)
        {
            _generaters[t] = generater;
        }
        internal Func<Closure, Delegate> GetGenerater(Type t)
        {
            if(_generaters.ContainsKey(t))
            {
                return _generaters[t];
            }
            return null;
        }
    }

    public class ImportManager
    {
        Dictionary<Type, IImportTypeHandler> _handlers = new Dictionary<Type, IImportTypeHandler>();

        internal IImportTypeHandler GetHandler(Type t)
        {
            if (_handlers.ContainsKey(t))
            {
                return _handlers[t];
            }
            return null;
        }

        internal IImportTypeHandler GetOrCreateHandler(Type t)
        {
            if (_handlers.ContainsKey(t))
            {
                return _handlers[t];
            }
            else
            {
                AddHandler(t);
                return _handlers[t];
            }
        }

        internal void AddHandler(Type t)
        {
            if (_handlers.ContainsKey(t))
            {
                return;
            }
            var handler = ImportTypeHandler.Create(t);
            _handlers.Add(t, handler);
        }

        public void RegisterHandler(Type t, IImportTypeHandler handler)
        {
            _handlers[t] = handler;
        }
    }
}

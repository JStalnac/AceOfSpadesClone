using System;

namespace Dash.CMD
{
        public class CVar
        {
            public Type dtype;
            public object value;

            public CVar(Type dtype, object value)
            {
                this.dtype = dtype;
                this.value = value;
            }
        }
}
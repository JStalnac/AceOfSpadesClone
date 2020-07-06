using System;

namespace Dash.CMD
{
    public static partial class DashCMD
    {
        /// <summary>
        /// Attemps to retrieve a CVar.
        /// </summary>
        /// <typeparam name="T">The datatype of the CVar.</typeparam>
        /// <param name="name">The name of the CVar.</param>
        /// <returns>The CVar, as it's actual datatype.</returns>
        public static T GetCVar<T>(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            CVar cvar;
            if (CVars.TryGetValue(name, out cvar))
                return (T)cvar.value;
            else
                throw new Exception(String.Format("CVar \"{0}\" does not exist!", name));
        }

        /// <summary>
        /// Trys to get a CVar.
        /// <para>Returns true if the CVar was found.</para>
        /// </summary>
        /// <typeparam name="T">The datatype of the CVar.</typeparam>
        /// <param name="name">The name of the CVar.</param>
        /// <param name="value">The value of the CVar.</param>
        /// <returns>Returns whether or not the CVar was found.</returns>
        public static bool TryGetCVar<T>(string name, out T value)
        {
            value = default(T);
            CVar cvar;

            bool success = CVars.TryGetValue(name, out cvar);
            value = (T)cvar.value;
            return success;
        }

        /// <summary>
        /// Sets or Adds a CVar.
        /// </summary>
        /// <typeparam name="T">The datatype of the cvar.</typeparam>
        /// <param name="name">The cvar's name.</param>
        /// <param name="value">The cvar's value.</param>
        public static void SetCVar<T>(string name, T value)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (value == null)
                throw new ArgumentNullException("value");

            if (CVars.ContainsKey(name))
                CVars[name] = new CVar(CVars[name].dtype, value);
            else
                CVars.Add(name, new CVar(typeof(T), value));
        }

        /// <summary>
        /// Sets a CVar (infers datatype, slower than SetCVar<T>).
        /// </summary>
        /// <param name="name">The cvar's name.</param>
        /// <param name="value">The cvar's value.</param>
        static void SetCVar(string name, object value)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (value == null)
                throw new ArgumentNullException("value");

            if (CVars.ContainsKey(name))
                CVars[name] = new CVar(CVars[name].dtype, Convert.ChangeType(value, CVars[name].dtype));
            else
                throw new Exception(
                    String.Format("CVar \"{0}\" doesnt exist, and cannot be added! (To add, use AddCVar Or SetCVar<T>)", name));
        }

        /// <summary>
        /// Gets whether or not the specified CVar exists.
        /// </summary>
        /// <param name="name">The name of the CVar.</param>
        public static bool IsCVarDefined(string name)
        {
            return CVars.ContainsKey(name);
        }

        /// <summary>
        /// Adds a CVar.
        /// </summary>
        /// <typeparam name="T">The datatype of the cvar.</typeparam>
        /// <param name="name">The cvar's name.</param>
        /// <param name="value">The cvar's value.</param>
        public static void AddCVar<T>(string name, T value)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (value == null)
                throw new ArgumentNullException("value");

            if (CVars.ContainsKey(name))
                throw new Exception(String.Format("CVar \"{0}\" already exists!", name));
            else
                CVars.Add(name, new CVar(typeof(T), value));
        }

        /// <summary>
        /// Removes a CVar.
        /// </summary>
        /// <param name="name">The name of the CVar</param>
        public static void RemoveCVar(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            if (CVars.ContainsKey(name))
                CVars.Remove(name);
            else
                throw new Exception(String.Format("Failed to remove CVar \"{0}\", it does not exist!", name));
        }        
    }
}
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Base_CityGeneration
{
    class Class1
    {
        public static object GetDefaultTask(Type returnType)
        {
            var genericTaskType = typeof(Task<>);
            if (!returnType.IsGenericType)
                throw new NotSupportedException("Not a Task<A>, what to do?");

            var gtd = returnType.GetGenericTypeDefinition();
            if (gtd != genericTaskType)
                throw new NotSupportedException("Not a Task<>, what to do?");

            //Assume 1 argument, #yolo
            var arg = returnType.GetGenericArguments()[0];
            var defVal = DefaultValue(arg);

            //A method which will return the default value for this type
            var defValMethod = typeof(Class1).GetMethod("DefaultValueGeneric", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(arg);

            //Make a func which returns this val
            var funType = typeof(Func<>).MakeGenericType(arg);
            var delegateForDefValMethod = Delegate.CreateDelegate(funType, defValMethod);

            var newTaskFun = typeof(Class1).GetMethod("StartNewTask", BindingFlags.NonPublic | BindingFlags.Static);
            var cNewTask = newTaskFun.MakeGenericMethod(arg);
            return cNewTask.Invoke(Task.Factory, new object [] { delegateForDefValMethod });
        }

        private static Task<T> StartNewTask<T>(Func<T> fun)
        {
            return Task<T>.Factory.StartNew(fun);
        }

        private static T DefaultValueGeneric<T>()
        {
            return default(T);
        }

        public static object DefaultValue(Type t)
        {
            return typeof(Class1).GetMethod("DefaultValueGeneric", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(t).Invoke(null, null);
        }
    }
}

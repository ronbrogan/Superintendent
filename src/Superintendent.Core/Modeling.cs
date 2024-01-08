
using Superintendent.Core.CommandSink;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

/*
 *
 *
 *
 *
 *   This file is auto generated
 *     Do not modify manually
 *        Edit the template
 *
 *
 *
 *
 *
 */

namespace Superintendent.Core
{
    public abstract class CommandSinkClient<TOffsets>
    {
        public readonly ICommandSink CommandSink;
        public readonly TOffsets Offsets;

        protected CommandSinkClient(ICommandSink sink, TOffsets offsets)
        {
            this.CommandSink = sink;
            this.Offsets = offsets;
        }
    }

    public class InvocationException : Exception { }

    public class Fun 
    {
        public Fun(nint address)
        {
            this.Address = address;
        }

        public nint Address { get; }

        public static implicit operator Fun(nint address) => new(address);
    }

    public class ParamNamesAttribute : Attribute
    {
        private readonly string[] names;

        public ParamNamesAttribute(params string[] names)
        {
            this.names = names;
        }
    }

    public class Fun<TRet> : Fun 
    {
        public Fun(nint address) : base(address) { } 
        public static implicit operator Fun<TRet>(nint address) => new (address); 
        public static implicit operator nint(Fun<TRet> fun) => fun.Address;
    }
    public class Fun<TArg1, TRet> : Fun 
    {
        public Fun(nint address) : base(address) { } 
        public static implicit operator Fun<TArg1, TRet>(nint address) => new (address); 
        public static implicit operator nint(Fun<TArg1, TRet> fun) => fun.Address;
    }
    public class Fun<TArg1, TArg2, TRet> : Fun 
    {
        public Fun(nint address) : base(address) { } 
        public static implicit operator Fun<TArg1, TArg2, TRet>(nint address) => new (address); 
        public static implicit operator nint(Fun<TArg1, TArg2, TRet> fun) => fun.Address;
    }
    public class Fun<TArg1, TArg2, TArg3, TRet> : Fun 
    {
        public Fun(nint address) : base(address) { } 
        public static implicit operator Fun<TArg1, TArg2, TArg3, TRet>(nint address) => new (address); 
        public static implicit operator nint(Fun<TArg1, TArg2, TArg3, TRet> fun) => fun.Address;
    }
    public class Fun<TArg1, TArg2, TArg3, TArg4, TRet> : Fun 
    {
        public Fun(nint address) : base(address) { } 
        public static implicit operator Fun<TArg1, TArg2, TArg3, TArg4, TRet>(nint address) => new (address); 
        public static implicit operator nint(Fun<TArg1, TArg2, TArg3, TArg4, TRet> fun) => fun.Address;
    }
    public class Fun<TArg1, TArg2, TArg3, TArg4, TArg5, TRet> : Fun 
    {
        public Fun(nint address) : base(address) { } 
        public static implicit operator Fun<TArg1, TArg2, TArg3, TArg4, TArg5, TRet>(nint address) => new (address); 
        public static implicit operator nint(Fun<TArg1, TArg2, TArg3, TArg4, TArg5, TRet> fun) => fun.Address;
    }
    public class Fun<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TRet> : Fun 
    {
        public Fun(nint address) : base(address) { } 
        public static implicit operator Fun<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TRet>(nint address) => new (address); 
        public static implicit operator nint(Fun<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TRet> fun) => fun.Address;
    }
    public class Fun<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TRet> : Fun 
    {
        public Fun(nint address) : base(address) { } 
        public static implicit operator Fun<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TRet>(nint address) => new (address); 
        public static implicit operator nint(Fun<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TRet> fun) => fun.Address;
    }

    public class FunVoid : Fun 
    {
        public FunVoid(nint address) : base(address) { } 
        public static implicit operator FunVoid(nint address) => new (address); 
        public static implicit operator nint(FunVoid fun) => fun.Address;
    }

    public class FunVoid<TArg1> : Fun 
    {
        public FunVoid(nint address) : base(address) { } 
        public static implicit operator FunVoid<TArg1>(nint address) => new (address); 
        public static implicit operator nint(FunVoid<TArg1> fun) => fun.Address;
    }
    public class FunVoid<TArg1, TArg2> : Fun 
    {
        public FunVoid(nint address) : base(address) { } 
        public static implicit operator FunVoid<TArg1, TArg2>(nint address) => new (address); 
        public static implicit operator nint(FunVoid<TArg1, TArg2> fun) => fun.Address;
    }
    public class FunVoid<TArg1, TArg2, TArg3> : Fun 
    {
        public FunVoid(nint address) : base(address) { } 
        public static implicit operator FunVoid<TArg1, TArg2, TArg3>(nint address) => new (address); 
        public static implicit operator nint(FunVoid<TArg1, TArg2, TArg3> fun) => fun.Address;
    }
    public class FunVoid<TArg1, TArg2, TArg3, TArg4> : Fun 
    {
        public FunVoid(nint address) : base(address) { } 
        public static implicit operator FunVoid<TArg1, TArg2, TArg3, TArg4>(nint address) => new (address); 
        public static implicit operator nint(FunVoid<TArg1, TArg2, TArg3, TArg4> fun) => fun.Address;
    }
    public class FunVoid<TArg1, TArg2, TArg3, TArg4, TArg5> : Fun 
    {
        public FunVoid(nint address) : base(address) { } 
        public static implicit operator FunVoid<TArg1, TArg2, TArg3, TArg4, TArg5>(nint address) => new (address); 
        public static implicit operator nint(FunVoid<TArg1, TArg2, TArg3, TArg4, TArg5> fun) => fun.Address;
    }
    public class FunVoid<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> : Fun 
    {
        public FunVoid(nint address) : base(address) { } 
        public static implicit operator FunVoid<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(nint address) => new (address); 
        public static implicit operator nint(FunVoid<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> fun) => fun.Address;
    }
    public class FunVoid<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> : Fun 
    {
        public FunVoid(nint address) : base(address) { } 
        public static implicit operator FunVoid<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(nint address) => new (address); 
        public static implicit operator nint(FunVoid<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> fun) => fun.Address;
    }


    public struct Ptr<T> where T : unmanaged
    {
        public T Value { get; set; }

        public Ptr(T value)
        {
            this.Value = value;
        }

        public static Ptr<T> From(T value)
        {
            return new Ptr<T>(value);
        }

        public static implicit operator nint(Ptr<T> ptr)
        {
            if (ptr is Ptr<nint> nintPtr)
                return nintPtr.Value;

            return 0;
        }
    }
}

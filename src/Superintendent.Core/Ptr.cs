namespace Superintendent.Core
{
    public struct Ptr<T> where T: unmanaged
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
            if(ptr is Ptr<nint> nintPtr)
                return nintPtr.Value;

            return 0;
        }
    }
}

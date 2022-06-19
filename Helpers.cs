using AngleSharp.Dom;
using System.Text.RegularExpressions;

public static class Helpers
{
    public static string RegexPageNumber(this string text)
    {
        Regex regex = new Regex(@"\d+");
        var match = regex.Match(text);
        if (match.Success)
            return match.Value;
        else
            return null;
    }

    public static string GetContent(this IElement? element, string ifNull = "") => (element != null) ? element.TextContent.Trim() : ifNull;
}

public class RWLock : IDisposable
{
    public RWLock(LockRecursionPolicy RecursionPolicy = LockRecursionPolicy.SupportsRecursion)
    {
        @lock = new ReaderWriterLockSlim(RecursionPolicy);
    }

    public struct WriteLockToken : IDisposable
    {
        private readonly ReaderWriterLockSlim @lock;
        public WriteLockToken(ReaderWriterLockSlim @lock)
        {
            this.@lock = @lock;
            @lock.EnterWriteLock();
        }
        public void Dispose() => @lock.ExitWriteLock();
    }

    public struct ReadLockToken : IDisposable
    {
        private readonly ReaderWriterLockSlim @lock;
        public ReadLockToken(ReaderWriterLockSlim @lock)
        {
            this.@lock = @lock;
            @lock.EnterReadLock();
        }
        public void Dispose() => @lock.ExitReadLock();
    }

    private readonly ReaderWriterLockSlim @lock;

    public ReadLockToken ReadLock() => new ReadLockToken(@lock);
    public WriteLockToken WriteLock() => new WriteLockToken(@lock);

    public void Dispose() => @lock.Dispose();
}
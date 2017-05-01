namespace QuickShare.DevicesListManager
{
    public interface IAttributesNormalizer
    {
        NormalizedRemoteSystem Normalize(object o);
    }
}
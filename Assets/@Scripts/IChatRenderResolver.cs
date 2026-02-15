public interface IChatRenderResolver
{
    ChatRenderModel Resolve(in ChatEvent evt);
}
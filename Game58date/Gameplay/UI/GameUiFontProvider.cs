#nullable enable
using System;
using Stride.Core;
using Stride.Core.Serialization.Contents;
using Stride.Graphics;

namespace Game58date.Gameplay.UI;

public static class GameUiFontProvider
{
    private const string FontAssetName = "UIFont";
    private static SpriteFont? cachedFont;

    public static SpriteFont Load(IServiceRegistry services)
    {
        if (cachedFont is not null)
        {
            return cachedFont;
        }

        IContentManager? content = services.GetService<IContentManager>();
        if (content is null)
        {
            throw new InvalidOperationException("IContentManager service is unavailable.");
        }

        cachedFont = content.Load<SpriteFont>(FontAssetName);
        return cachedFont;
    }
}

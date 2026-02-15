using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum TextLayout { Big, Medium, Small }

public interface IUIPatch
{
    void Apply(UIBase ui, string portId);
}

public class TextPatch : IUIPatch
{
    private readonly TextLayout _textLayout;
    public TextPatch(TextLayout textLayout)
    {
        _textLayout = textLayout;
    }
    
    public void Apply(UIBase ui, string portId)
    { }
}

public readonly struct SpritePortBinding
{
    public readonly string PortId;   // ex) "TimelineBG_Image"
    public readonly string Address;  // ex) "ui/common/TimelineBG_Image"

    public SpritePortBinding(string portId, string address)
    {
        PortId  = portId;
        Address = address;
    }
}
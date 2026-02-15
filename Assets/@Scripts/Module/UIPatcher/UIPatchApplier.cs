using System.Collections.Generic;

public class UIPatchApplier
{
    public void Apply(UIBase ui, string portId,  List<IUIPatch> patches)
    {
        foreach (IUIPatch uiPatch in patches)
        {
            uiPatch.Apply(ui, portId);
        }
    }
}
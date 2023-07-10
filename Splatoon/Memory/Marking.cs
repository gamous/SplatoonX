using ECommons.GameFunctions;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace Splatoon.Memory;

public class Marking
{
    public unsafe static long GetMarker(uint index) => MarkingController.Instance()->MarkerArray[index];

    public unsafe static bool HaveMark(Character obj, uint index)
    {
        if (obj.Struct()->ModelCharaId!=0)
        {
            if (Svc.ClientState.LocalPlayer.ObjectId == GetMarker(index))
            {
                return true;
            }
        }
        else
        {
            if (obj.ObjectId == GetMarker(index))
            {
                return true;
            }
        }
        
        return false;
    }
    private Dictionary<long, string> markers = new Dictionary<long, string>()
    {
        { GetMarker(0), "攻击1" },
        { GetMarker(1), "攻击2" },
        { GetMarker(2), "攻击3" },
        { GetMarker(3), "攻击4" },
        { GetMarker(4), "攻击5" },
        { GetMarker(5), "锁链1" },
        { GetMarker(6), "锁链2" },
        { GetMarker(7), "锁链3" },
        { GetMarker(8), "禁止1" },
        { GetMarker(9), "禁止2" },
        { GetMarker(10), "方块" },
        { GetMarker(11), "圆圈" },
        { GetMarker(12), "十字" },
        { GetMarker(13), "三角" }
    };

    public string Mark(uint objectid)
    {
        if (markers.TryGetValue(objectid, out string attack))
        {
            return attack;
        }
        else
        {
            return "无标记";
        }
    }

    public static unsafe GameObject GetPlayer(int x)
    {
        var ph = FakePronoun.Resolve($"<{x}>");
        if (ph != null)
        {
            var obj = Svc.Objects.CreateObjectReference((nint)ph);
            return obj;
        }
        return Svc.ClientState.LocalPlayer;
    }
}
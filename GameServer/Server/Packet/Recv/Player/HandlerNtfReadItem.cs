using Google.Protobuf;
using MikuSB.Database;
using MikuSB.Database.Inventory;
using MikuSB.Enums.Item;
using MikuSB.Proto;
using MikuSB.Util;

namespace MikuSB.GameServer.Server.Packet.Recv.Login;

[Opcode(CmdIds.NtfReadItem)]
public class HandlerNtfReadItem : Handler
{
    public override async Task OnHandle(Connection connection, byte[] data, ushort seqNo)
    {
        var req = IDArray.Parser.ParseFrom(data);
        var json = JsonFormatter.Default.Format(req);
        Logger.GetByClassName().Debug($"{json}");

        if (req.Ids.Count == 0)
            return;

        var player = connection.Player!;
        var changedCharacters = false;
        var changedInventory = false;
        var readFlag = (uint)ItemFlagEnum.FLAG_READED;

        foreach (var id in req.Ids)
        {
            var character = player.CharacterManager.CharacterData.Characters
                .FirstOrDefault(x => x.Guid == id);
            if (character != null && (((uint)character.Flag & readFlag) == 0))
            {
                character.Flag = (ItemFlagEnum)((uint)character.Flag | readFlag);
                changedCharacters = true;
                continue;
            }

            if (id > uint.MaxValue)
                continue;

            var itemId = (uint)id;
            if (MarkRead(player.InventoryManager.InventoryData.Items.GetValueOrDefault(itemId)))
            {
                changedInventory = true;
                continue;
            }

            if (MarkRead(player.InventoryManager.InventoryData.Weapons.GetValueOrDefault(itemId)))
            {
                changedInventory = true;
                continue;
            }

            if (MarkRead(player.InventoryManager.InventoryData.Skins.GetValueOrDefault(itemId)))
            {
                changedInventory = true;
                continue;
            }

            if (MarkRead(player.InventoryManager.InventoryData.SupportCards.GetValueOrDefault(itemId)))
                changedInventory = true;
        }

        if (changedCharacters)
            DatabaseHelper.SaveDatabaseType(player.CharacterManager.CharacterData);

        if (changedInventory)
            DatabaseHelper.SaveDatabaseType(player.InventoryManager.InventoryData);

        if (changedCharacters || changedInventory)
            await player.OnHeartBeat();
    }

    private static bool MarkRead(BaseGameItemInfo? item)
    {
        if (item == null)
            return false;

        var readFlag = (uint)ItemFlagEnum.FLAG_READED;
        if (((uint)item.Flag & readFlag) != 0)
            return false;

        item.Flag = (ItemFlagEnum)((uint)item.Flag | readFlag);
        return true;
    }
}

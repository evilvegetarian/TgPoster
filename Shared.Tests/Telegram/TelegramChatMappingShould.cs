using Shouldly;
using TgPoster.Telegram.Internal.Mapping;
using TgPoster.Telegram.Models;
using TL;

namespace Shared.Tests.Telegram;

public sealed class TelegramChatMappingShould
{
	[Fact]
	public void ForbidMemberPostingToBroadcastChannel()
	{
		var result = ChatMapper.FromChannel(new Channel
		{
			id = 123, access_hash = 456, title = "Channel", username = "channel", flags = Channel.Flags.broadcast,
			participants_count = 100
		});

		result.Id.ShouldBe(123);
		result.AccessHash.ShouldBe(456);
		result.Title.ShouldBe("Channel");
		result.ParticipantsCount.ShouldBe(100);
		result.CanSendMessages.ShouldBeFalse();
		result.CanSendMedia.ShouldBeFalse();
		result.MembersCanSendMessages.ShouldBeFalse();
		result.Peer.ShouldBe(TelegramPeer.Channel(123, 456));
	}

	[Fact]
	public void AllowCreatorPosting_DespiteMemberRestrictions()
	{
		var result = ChatMapper.FromChannel(new Channel
		{
			flags = Channel.Flags.broadcast | Channel.Flags.creator,
			default_banned_rights = new ChatBannedRights { flags = ChatBannedRights.Flags.send_messages | ChatBannedRights.Flags.send_media }
		});

		result.IsCreator.ShouldBeTrue();
		result.IsAdmin.ShouldBeTrue();
		result.CanSendMessages.ShouldBeTrue();
		result.CanSendMedia.ShouldBeTrue();
		result.IsAccountRestricted.ShouldBeFalse();
	}

	[Theory]
	[InlineData(ChatAdminRights.Flags.post_messages, true)]
	[InlineData(ChatAdminRights.Flags.manage_call, true)]
	[InlineData((ChatAdminRights.Flags)0, false)]
	public void RespectAdministratorPostingRights(ChatAdminRights.Flags rights, bool canPost)
	{
		var result = ChatMapper.FromChannel(new Channel
		{
			flags = Channel.Flags.broadcast, admin_rights = new ChatAdminRights { flags = rights }
		});

		result.IsAdmin.ShouldBeTrue();
		result.CanSendMessages.ShouldBe(canPost);
		result.CanSendMedia.ShouldBeTrue();
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void DistinguishPersonalRestrictionsFromGroupDefaults(bool personal)
	{
		var rights = new ChatBannedRights { flags = ChatBannedRights.Flags.send_messages | ChatBannedRights.Flags.send_media };
		var result = ChatMapper.FromChannel(new Channel
		{
			flags = Channel.Flags.megagroup, banned_rights = personal ? rights : null, default_banned_rights = personal ? null : rights
		});

		result.CanSendMessages.ShouldBeFalse();
		result.CanSendMedia.ShouldBeFalse();
		result.IsAccountRestricted.ShouldBe(personal);
		result.MembersCanSendMessages.ShouldBe(personal);
		result.MembersCanSendMedia.ShouldBe(personal);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void RespectBasicGroupDeactivation(bool deactivated)
	{
		var result = ChatMapper.FromChat(new Chat { id = 100, title = "Group", flags = deactivated ? Chat.Flags.deactivated : 0 });

		result.IsGroup.ShouldBeTrue();
		result.CanSendMessages.ShouldBe(!deactivated);
		result.CanSendMedia.ShouldBe(!deactivated);
		result.ParticipantsCount.ShouldBeNull();
		result.Peer.ShouldBe(TelegramPeer.Chat(100));
	}

	[Fact]
	public void UseUserNameOrFullName_AndRecognizeBots()
	{
		var named = ChatMapper.FromUser(new User { id = 1, access_hash = 2, username = "user" });
		var bot = ChatMapper.FromUser(new User { id = 3, first_name = "Test", last_name = "Bot", flags = User.Flags.bot });

		named.Title.ShouldBe("user");
		named.CanSendMessages.ShouldBeTrue();
		named.Peer.ShouldBe(TelegramPeer.User(1, 2));
		bot.Title.ShouldBe("Test Bot");
		bot.CanSendMessages.ShouldBeFalse();
	}

	[Fact]
	public void IgnoreForbiddenChats_WhenMappingPage()
	{
		var page = MessageMapper.MapPage(new Messages_Messages
		{
			messages = [new Message { id = 10, message = "Text" }, new MessageService { id = 11 }],
			chats = new Dictionary<long, ChatBase>
			{
				[100] = new Chat { id = 100, title = "Group" }, [200] = new Channel { id = 200, title = "Channel" },
				[300] = new ChatForbidden { id = 300 }
			},
			users = []
		});

		page.Messages.Select(message => message.Id).ShouldBe([10]);
		page.Chats.Select(chat => chat.Id).Order().ShouldBe([100L, 200L]);
		ChatMapper.SafeMap(new ChannelForbidden { id = 300 }).ShouldBeNull();
	}
}

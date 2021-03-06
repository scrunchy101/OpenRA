﻿#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Gives additional cash when resources are delivered to refineries.")]
	public class ResourcePurifierInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Percentage value of the resource to grant as cash.")]
		public readonly int Modifier = 0;

		[Desc("Whether to show the cash tick indicators rising from the actor.")]
		public readonly bool ShowTicks = true;

		[Desc("How long the cash ticks stay on the screen.")]
		public readonly int TickLifetime = 30;

		[Desc("How often the cash ticks can appear.")]
		public readonly int TickRate = 10;

		public override object Create(ActorInitializer init) { return new ResourcePurifier(init.Self, this); }
	}

	public class ResourcePurifier : ConditionalTrait<ResourcePurifierInfo>, INotifyCreated, INotifyResourceAccepted, ITick, INotifyOwnerChanged
	{
		readonly int[] modifier;

		PlayerResources playerResources;
		int currentDisplayTick;
		int currentDisplayValue;

		public ResourcePurifier(Actor self, ResourcePurifierInfo info)
			: base(info)
		{
			modifier = new int[] { Info.Modifier };
			currentDisplayTick = Info.TickRate;
		}

		void INotifyCreated.Created(Actor self)
		{
			// Special case handling is required for the Player actor.
			// Created is called before Player.PlayerActor is assigned,
			// so we must query other player traits from self, knowing that
			// it refers to the same actor as self.Owner.PlayerActor
			var playerActor = self.Info.Name == "player" ? self : self.Owner.PlayerActor;

			playerResources = playerActor.Trait<PlayerResources>();
		}

		void INotifyResourceAccepted.OnResourceAccepted(Actor self, Actor refinery, int amount)
		{
			if (IsTraitDisabled)
				return;

			var cash = Util.ApplyPercentageModifiers(amount, modifier);
			playerResources.GiveCash(cash);

			if (Info.ShowTicks && self.Info.HasTraitInfo<IOccupySpaceInfo>())
				currentDisplayValue += cash;
		}

		void ITick.Tick(Actor self)
		{
			if (currentDisplayValue > 0 && --currentDisplayTick <= 0)
			{
				var temp = currentDisplayValue;
				if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
					self.World.AddFrameEndTask(w => w.Add(new FloatingText(self.CenterPosition, self.Owner.Color, FloatingText.FormatCashTick(temp), Info.TickLifetime)));

				currentDisplayTick = Info.TickRate;
				currentDisplayValue = 0;
			}
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			playerResources = newOwner.PlayerActor.Trait<PlayerResources>();
			currentDisplayTick = Info.TickRate;
			currentDisplayValue = 0;
		}
	}
}

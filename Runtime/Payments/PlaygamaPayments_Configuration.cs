using MirraGames.SDK.Common;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MirraGames.SDK.Playgama {

    [ProviderConfiguration(typeof(PlaygamaPayments))]
    public class PlaygamaPayments_Configuration : PropertyGroup {

        public override string Name => nameof(PlaygamaPayments);

        /// <summary>
        /// Comma separated product tags that are consumed right after they are granted, so they can be bought again.
        /// Every other product is permanent and is restored on each launch.
        /// </summary>
        [field: SerializeField] public string ConsumableProductTags { get; private set; } = string.Empty;

        public HashSet<string> GetConsumableTags() {
            HashSet<string> tags = new();
            if (string.IsNullOrEmpty(ConsumableProductTags)) {
                return tags;
            }
            foreach (string tag in ConsumableProductTags.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)) {
                string trimmed = tag.Trim();
                if (trimmed.Length > 0) {
                    tags.Add(trimmed);
                }
            }
            return tags;
        }

        public override StringProperty[] GetStringProperties() {
            return new StringProperty[] {
                new(
                    nameof(ConsumableProductTags),
                    getter: () => ConsumableProductTags,
                    setter: (value) => ConsumableProductTags = value
                )
            };
        }

    }

}

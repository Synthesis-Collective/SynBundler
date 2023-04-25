using System;
using System.Threading.Tasks;

using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;

using Noggog;

namespace SynBundler
{
    internal class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "SynBundler.esp")
                .Run(args);
        }
        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            state.LoadOrder.PriorityOrder.Ammunition().WinningOverrides().ForEach(abt =>
            {
                if (abt.HasKeyword(Skyrim.Keyword.VendorItemArrow) && (!abt.Name?.String.IsNullOrEmpty() ?? false))
                {
                    var miscitem = state.PatchMod.MiscItems.AddNew($"bundled_{abt.EditorID}");
                    miscitem.Model = abt.Model?.DeepCopy();
                    miscitem.Keywords = new();
                    miscitem.Keywords?.Add(Skyrim.Keyword.VendorItemArrow);
                    miscitem.Name = $"Bundle of {abt.Name}";
                    miscitem.Value = 10 * abt.Value;
                    Console.WriteLine($"Generating {miscitem.Name}");
                    var bundler = state.PatchMod.ConstructibleObjects.AddNew($"bundle_{abt.EditorID}");
                    bundler.CreatedObject.SetTo(miscitem);
                    bundler.CreatedObjectCount = 1;
                    bundler.Items = new ExtendedList<ContainerEntry>
                    {
                        new ContainerEntry()
                        {
                            Item = new ContainerItem()
                            {
                                Item = abt.ToLink(),
                                Count = 10
                            }
                        }
                    };
                    var DataBundle = new GetItemCountConditionData();
                    DataBundle.ItemOrList.Link.SetTo(abt.FormKey);
                    bundler.WorkbenchKeyword.SetTo(Skyrim.Keyword.CraftingTanningRack);
                    bundler.Conditions.Add(new ConditionFloat()
                    {
                        CompareOperator = CompareOperator.GreaterThanOrEqualTo,
                        ComparisonValue = 10,
                        Data = DataBundle,
                    });
                    var unbundler = state.PatchMod.ConstructibleObjects.AddNew($"unbundle_{abt.EditorID}");
                    unbundler.CreatedObject.SetTo(abt);
                    unbundler.CreatedObjectCount = 10;
                    unbundler.Items = new ExtendedList<ContainerEntry>
                    {
                        new ContainerEntry()
                        {
                            Item = new ContainerItem()
                            {
                                Item = miscitem.ToLink(),
                                Count = 1
                            }
                        }
                    };
                    var DataUnbundle = new GetItemCountConditionData();
                    DataUnbundle.ItemOrList.Link.SetTo(abt.FormKey);
                    unbundler.WorkbenchKeyword.SetTo(Skyrim.Keyword.CraftingTanningRack);
                    unbundler.Conditions.Add(new ConditionFloat()
                    {
                        CompareOperator = CompareOperator.GreaterThanOrEqualTo,
                        ComparisonValue = 1,
                        Data = DataUnbundle,
                    });
                }
            });
        }
    }
}
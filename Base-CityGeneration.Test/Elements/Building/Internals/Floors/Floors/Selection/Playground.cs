using System;
using System.IO;
using Base_CityGeneration.Elements.Building.Internals.Floors.Selection;
using EpimetheusPlugins.Scripts;
using EpimetheusPlugins.Testing.MockScripts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Base_CityGeneration.Test.Elements.Building.Internals.Floors.Floors.Selection
{
    [TestClass]
    public class Playground
    {
        [TestMethod]
        public void TestMethod1()
        {
            var b = FloorSelector.Deserialize(new StringReader(@"
!Building
Floors:
  - !Floor
    Tags:
      1:  [Roof Garden, Helipad]
      99: [Boring Roof, No Helepad]
      0:  [blank]

  - !Floor
    Tags:
      50: [ Penthouse ]
      50: null

  - !Range
    DefaultHeight:
        Min: 2
        Max: 3
        Vary: false
    Includes:
      - AtLeast: 1
        AtMost: 1
        Tags:
          1: [ common floor ]

      - AtLeast: 0
        AtMost: 1
        Tags:
          1: [ secret mafia storage floor ]
          99: [ storage ]

      - AtLeast: 1
        Mean: 5
        AtMost: 10
        Deviation: 2
        Continuous: false
        Vary: false
        Tags:
          1: [ residential, apartments ]

  - !Floor
    Tags:
      100: [ Ground, Shop, Jewellers ]

  - !Ground {}

  - !Range
    Includes:
      - AtMost: 1
        Tags:
          1: [ Basement, Bat Cave ]
          1: [ basement, illegal sweatshop ]
          98: null
      - Tags:
          1: [ basement, storage ]
          1: [ basement, workshop ]
"));

            Assert.IsNotNull(b);

            Func<string[], ScriptReference> finder = tags => ScriptReferenceFactory.Create(typeof(TestScript), Guid.NewGuid(), string.Join(",", tags));

            Random r = new Random();
            var selection = b.Select(r.NextDouble, finder);

            foreach (var item in selection.AboveGroundFloors)
                Console.WriteLine("{0} {1:##.##}m", item.Script.Name, item.Height);
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.IO;
using Base_CityGeneration.Elements.Roads.Hyperstreamline.Tracing;
using Base_CityGeneration.Utilities.Numbers;
using Myre.Collections;
using SharpYaml.Serialization;

namespace Base_CityGeneration.Elements.Roads.Hyperstreamline
{
    public class NetworkDescriptor
    {
        private readonly TracingConfiguration.Container _major;
        private readonly TracingConfiguration.Container _minor;

        private NetworkDescriptor(TracingConfiguration.Container major, TracingConfiguration.Container minor)
        {
            _major = major;
            _minor = minor;
        }

        public TracingConfiguration Major(Func<double> random, INamedDataCollection metadata)
        {
            return _major.Unwrap(random, metadata);
        }

        public TracingConfiguration Minor(Func<double> random, INamedDataCollection metadata)
        {
            return _minor.Unwrap(random, metadata);
        }

        #region serialization
        private static Serializer CreateSerializer()
        {
            var serializer = new Serializer(new SerializerSettings
            {
                EmitTags = true,
            });

            //Fields Types
            serializer.Settings.RegisterTagMapping("ConstantScalars", typeof(Fields.Scalars.Constant.Container));
            //serializer.Settings.RegisterTagMapping("ConstantTensors", typeof(Fields.Tensors.Constant.Container));
            //serializer.Settings.RegisterTagMapping("ConstantVectors", typeof(Fields.Vectors.Constant.Container));
            serializer.Settings.RegisterTagMapping("AddTensors", typeof(Fields.Tensors.Addition.Container));
            serializer.Settings.RegisterTagMapping("PointDistanceDecayTensors", typeof(Fields.Tensors.PointDistanceDecayField.Container));
            serializer.Settings.RegisterTagMapping("Radial", typeof(Fields.Tensors.Radial.Container));
            serializer.Settings.RegisterTagMapping("Grid", typeof(Fields.Tensors.Gridline.Container));
            serializer.Settings.RegisterTagMapping("Polyline", typeof(Fields.Tensors.Polyline.Container));
            serializer.Settings.RegisterTagMapping("WeightedAverage", typeof(Fields.Tensors.WeightedAverage.Container));

            //Network Types
            serializer.Settings.RegisterTagMapping("Network", typeof(Container));

            //Utility types
            serializer.Settings.RegisterTagMapping("NormalValue", typeof(NormallyDistributedValue.Container));
            serializer.Settings.RegisterTagMapping("UniformValue", typeof(UniformlyDistributedValue.Container));
            serializer.Settings.RegisterTagMapping("ConstantValue", typeof(ConstantValue.Container));

            return serializer;
        }

        public static NetworkDescriptor Deserialize(TextReader reader)
        {
            var s = CreateSerializer();

            return s.Deserialize<Container>(reader).Unwrap();
        }

        internal class Container
        {
            public List<object> Aliases { get; set; }

            public TracingConfiguration.Container Major { get; set; }

            public TracingConfiguration.Container Minor { get; set; }

            public NetworkDescriptor Unwrap()
            {
                return new NetworkDescriptor(Major, Minor);
            }
        }
        #endregion
    }
}

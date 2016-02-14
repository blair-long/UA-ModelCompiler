/* ========================================================================
 * Copyright (c) 2005-2016 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Reciprocal Community License ("RCL") Version 1.00
 *
 * Unless explicitly acquired and licensed from Licensor under another
 * license, the contents of this file are subject to the Reciprocal
 * Community License ("RCL") Version 1.00, or subsequent versions
 * as allowed by the RCL, and You may not copy or use this file in either
 * source code or executable form, except in compliance with the terms and
 * conditions of the RCL.
 *
 * All software distributed under the RCL is provided strictly on an
 * "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESS OR IMPLIED,
 * AND LICENSOR HEREBY DISCLAIMS ALL SUCH WARRANTIES, INCLUDING WITHOUT
 * LIMITATION, ANY WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
 * PURPOSE, QUIET ENJOYMENT, OR NON-INFRINGEMENT. See the RCL for specific
 * language governing rights and limitations under the RCL.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/RCL/1.00/
 * ======================================================================*/

using System;
using System.Xml;
using System.Collections.Generic;
using System.ServiceModel;
using System.Runtime.Serialization;

namespace Opc.Ua
{
    /// <summary>
	/// A concrete base class used by the autogenerated code.
	/// </summary>
    [DataContract(Name = "EncodeableObject", Namespace = Namespaces.OpcUaXsd)]
	public abstract class EncodeableObject : IEncodeable
	{
        #region IEncodeable Methods
        /// <summary cref="IEncodeable.TypeId" />
        public abstract ExpandedNodeId TypeId { get; }

        /// <summary cref="IEncodeable.BinaryEncodingId" />
        public abstract ExpandedNodeId BinaryEncodingId { get; }

        /// <summary cref="IEncodeable.XmlEncodingId" />
        public abstract ExpandedNodeId XmlEncodingId { get; }

        /// <summary cref="IEncodeable.Encode" />
        public virtual void Encode(IEncoder encoder) {}

        /// <summary cref="IEncodeable.Decode" />
		public virtual void Decode(IDecoder decoder) {}

        /// <summary>
        /// Checks if the value has changed.
        /// </summary>
        public virtual bool IsEqual(IEncodeable encodeable)
        {
            throw new NotImplementedException("Subclass must implement this method.");
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Applies the data encoding to the value.
        /// </summary>
        public static ServiceResult ApplyDataEncoding(ServiceMessageContext context, QualifiedName dataEncoding, ref object value)
        {
            // check if nothing to do.
            if (QualifiedName.IsNull(dataEncoding) || value == null)
            {
                return ServiceResult.Good;
            }

            // check for supported encoding type.
            if (dataEncoding.NamespaceIndex != 0)
            {
                return StatusCodes.BadDataEncodingUnsupported;
            }

            bool useXml = dataEncoding.Name == DefaultXml;

            if (!useXml && dataEncoding.Name != DefaultBinary)
            {
                return StatusCodes.BadDataEncodingUnsupported;
            }

            try
            {
                // check for array of encodeables.
                IList<IEncodeable> encodeables = value as IList<IEncodeable>;

                if (encodeables == null)
                {
                    // check for array of extension objects.
                    IList<ExtensionObject> extensions = value as IList<ExtensionObject>;

                    if (extensions != null)
                    {
                        // convert extension objects to encodeables.
                        encodeables = new IEncodeable[extensions.Count];

                        for (int ii = 0; ii < encodeables.Count; ii++)
                        {
                            if (ExtensionObject.IsNull(extensions[ii]))
                            {
                                encodeables[ii] = null;
                                continue;
                            }

                            IEncodeable element = extensions[ii].Body as IEncodeable;

                            if (element == null)
                            {
                                return StatusCodes.BadTypeMismatch;
                            }

                            encodeables[ii] = element;
                        }
                    }
                }

                // apply data encoding to the array.
                if (encodeables != null)
                {
                    ExtensionObject[] extensions = new ExtensionObject[encodeables.Count];

                    for (int ii = 0; ii < extensions.Length; ii++)
                    {
                        extensions[ii] = Encode(context, encodeables[ii], useXml);
                    }

                    value = extensions;
                    return ServiceResult.Good;
                }

                // check for scalar value.
                IEncodeable encodeable = value as IEncodeable;

                if (encodeable == null)
                {
                    ExtensionObject extension = value as ExtensionObject;

                    if (extension == null)
                    {
                        return StatusCodes.BadDataEncodingUnsupported;
                    }

                    encodeable = extension.Body as IEncodeable;
                }

                if (encodeable == null)
                {
                    return StatusCodes.BadDataEncodingUnsupported;
                }

                // do conversion.
                value = Encode(context, encodeable, useXml);
                return ServiceResult.Good;
            }
            catch (Exception e)
            {
                return ServiceResult.Create(e, StatusCodes.BadTypeMismatch, "Could not convert value to requested format.");
            }
        }

        /// <summary>
        /// Encodes the object in XML or Binary
        /// </summary>
        public static ExtensionObject Encode(ServiceMessageContext context, IEncodeable encodeable, bool useXml)
        {
            if (useXml)
            {
                XmlElement body = EncodeableObject.EncodeXml(encodeable, context);
                return new ExtensionObject(encodeable.XmlEncodingId, body);
            }
            else
            {
                byte[] body = EncodeableObject.EncodeBinary(encodeable, context);
                return new ExtensionObject(encodeable.BinaryEncodingId, body);
            }
        }

        /// <summary>
        /// Encodes the object in XML.
        /// </summary>
        public static XmlElement EncodeXml(IEncodeable encodeable, ServiceMessageContext context)
        {
            // create encoder.
            XmlEncoder encoder = new XmlEncoder(context);

            // write body.
            encoder.WriteExtensionObjectBody(encodeable);

            // create document from encoder.
            XmlDocument document = new XmlDocument();
            document.InnerXml = encoder.Close();

            // return root element.
            return document.DocumentElement;
        }

        /// <summary>
        /// Encodes the object in binary
        /// </summary>
        public static byte[] EncodeBinary(IEncodeable encodeable, ServiceMessageContext context)
        {
            BinaryEncoder encoder = new BinaryEncoder(context);
            encoder.WriteEncodeable(null, encodeable, null);
            return encoder.CloseAndReturnBuffer();
        }

        /// <summary>
        /// The BrowseName for the DefaultBinary component.
        /// </summary>
        private const string DefaultBinary = "Default Binary";

        /// <summary>
        /// The BrowseName for the DefaultXml component.
        /// </summary>
        private const string DefaultXml = "Default XML";
        #endregion

        #region ICloneable Methods
        /// <summary>
		/// Returns a deep copy of an encodeable object.
		/// </summary>
        public virtual object Clone()
        {
            return MemberwiseClone();
        }
        #endregion
    }
}

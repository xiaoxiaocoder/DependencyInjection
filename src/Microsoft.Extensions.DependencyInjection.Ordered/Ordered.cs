// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection.Ordered
{
    internal class Ordered<T>: IOrdered<T>, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly OrderedServiceDescriptorContainer<T> _descriptorContainer;
        private List<T> _values;
        private List<IDisposable> _dispose;

        public Ordered(IServiceProvider serviceProvider, OrderedServiceDescriptorContainer<T> descriptorContainer)
        {
            _serviceProvider = serviceProvider;
            _descriptorContainer = descriptorContainer;
        }

        private void EnsureValues()
        {
            lock (_descriptorContainer)
            {
                if (_values != null)
                {
                    return;
                }

                _values = new List<T>();
                _dispose = new List<IDisposable>();
                foreach (var descriptor in _descriptorContainer.ServiceDescriptor.Descriptors)
                {
                    var factoryServiceDescriptor = descriptor as FactoryServiceDescriptor;
                    T value;
                    IDisposable disposable = null;

                    if (factoryServiceDescriptor != null)
                    {
                        value = (T)factoryServiceDescriptor.ImplementationFactory(_serviceProvider);
                        disposable = value as IDisposable;
                    }
                    else
                    {
                        var typeServiceDescriptor = descriptor as TypeServiceDescriptor;
                        if (typeServiceDescriptor != null)
                        {
                            value = (T) ActivatorUtilities.CreateInstance(_serviceProvider, typeServiceDescriptor.ImplementationType);
                            disposable = value as IDisposable;
                        }
                        else
                        {
                            var instanceServiceDescriptor = descriptor as InstanceServiceDescriptor;
                            if (instanceServiceDescriptor != null)
                            {
                                value = (T) instanceServiceDescriptor.ImplementationInstance;
                            }
                            else
                            {
                                throw new NotSupportedException($"Unsupported service descriptor type '{descriptor.GetType()}'");
                            }
                        }
                    }
                    _values.Add(value);
                    if (disposable != null)
                    {
                        _dispose.Add(disposable);
                    }
                }
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            EnsureValues();
            return _values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            lock (_descriptorContainer)
            {
                if (_dispose != null)
                {
                    foreach (var value in _dispose)
                    {
                        value?.Dispose();
                    }
                }
            }
        }
    }
}
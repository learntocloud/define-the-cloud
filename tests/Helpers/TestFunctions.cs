using Microsoft.Azure.Cosmos;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace cloud_dictionary.tests.Helpers;
public static class TestExtensions
{
    public static Mock<ItemResponse<T>> SetupCreateItemAsync<T>(this Mock<Container> containerMock)
    {
        var itemResponseMock = new Mock<ItemResponse<T>>();

        containerMock
            .Setup(x => x.CreateItemAsync(
                It.IsAny<T>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback(
                (T item, PartitionKey? pk, ItemRequestOptions opt, CancellationToken ct) => itemResponseMock.Setup(x => x.Resource).Returns(null))
            .ReturnsAsync(
                (T item, PartitionKey? pk, ItemRequestOptions opt, CancellationToken ct) => itemResponseMock.Object);

        return itemResponseMock;
    }

    public static Mock<ItemResponse<T>> SetupDeleteItemAsync<T>(this Mock<Container> containerMock)
    {
        var itemResponseMock = new Mock<ItemResponse<T>>();

        containerMock
            .Setup(x => x.DeleteItemAsync<T>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback(
                (string id, PartitionKey? pk, ItemRequestOptions opt, CancellationToken ct) => itemResponseMock.Setup(x => x.Resource).Returns(null))
            .ReturnsAsync(
                (string id, PartitionKey? pk, ItemRequestOptions opt, CancellationToken ct) => itemResponseMock.Object);

        return itemResponseMock;
    }

    public static Mock<ItemResponse<T>> SetupReadItemAsync<T>(this Mock<Container> containerMock, T objectToReturn)
    {
        var itemResponseMock = new Mock<ItemResponse<T>>();
        itemResponseMock.Setup(x => x.Resource).Returns(objectToReturn);

        containerMock
            .Setup(x => x.ReadItemAsync<T>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(itemResponseMock.Object);

        return itemResponseMock;
    }
}
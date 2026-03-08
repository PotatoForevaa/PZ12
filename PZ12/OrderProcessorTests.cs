using Moq;
using Xunit;

namespace PZ12
{
    public class OrderProcessorTests
    {
        private readonly Mock<IDatabase> _databaseMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly OrderProcessor _processor;

        public OrderProcessorTests()
        {
            _databaseMock = new Mock<IDatabase>();
            _emailServiceMock = new Mock<IEmailService>();

            _processor = new OrderProcessor(_databaseMock.Object, _emailServiceMock.Object);
        }

        [Fact]
        public void ProcessOrder_ValidOrder_ReturnsTrue()
        {
            var order = new Order { Id = 1, CustomerEmail = "test@mail.com", TotalAmount = 50 };
            _databaseMock.Setup(x => x.IsConnected).Returns(true);

            var result = _processor.ProcessOrder(order);

            Assert.True(result);
            Assert.True(order.IsProcessed);
            _databaseMock.Verify(x => x.Save(order), Times.Once);
        }

        [Fact]
        public void ProcessOrder_OrderIsNull_ThrowsArgumentNullException()
        {
            Order order = null;

            Assert.Throws<ArgumentNullException>(() => _processor.ProcessOrder(order));
        }

        [Fact]
        public void ProcessOrder_TotalAmountLessOrEqualZero_ReturnsFalse()
        {
            var order = new Order { Id = 1, CustomerEmail = "test@mail.com", TotalAmount = 0 };

            var result = _processor.ProcessOrder(order);

            Assert.False(result);
            _databaseMock.Verify(x => x.Save(It.IsAny<Order>()), Times.Never);
        }

        [Fact]
        public void ProcessOrder_DatabaseNotConnected_CallsConnect()
        {
            var order = new Order { Id = 1, CustomerEmail = "test@mail.com", TotalAmount = 50 };
            _databaseMock.Setup(x => x.IsConnected).Returns(false);

            _processor.ProcessOrder(order);

            _databaseMock.Verify(x => x.Connect(), Times.Once);
        }

        [Fact]
        public void ProcessOrder_TotalAmountGreaterThan100_SendsEmail()
        {
            var order = new Order { Id = 2, CustomerEmail = "test@mail.com", TotalAmount = 150 };
            _databaseMock.Setup(x => x.IsConnected).Returns(true);

            _processor.ProcessOrder(order);

            _emailServiceMock.Verify(x =>
                x.SendOrderConfirmation(order.CustomerEmail, order.Id),
                Times.Once);
        }

        [Fact]
        public void ProcessOrder_TotalAmountLessOrEqual100_DoesNotSendEmail()
        {
            var order = new Order { Id = 3, CustomerEmail = "test@mail.com", TotalAmount = 100 };
            _databaseMock.Setup(x => x.IsConnected).Returns(true);

            _processor.ProcessOrder(order);

            _emailServiceMock.Verify(x =>
                x.SendOrderConfirmation(It.IsAny<string>(), It.IsAny<int>()),
                Times.Never);
        }

        [Fact]
        public void ProcessOrder_DatabaseSaveThrowsException_ReturnsFalse()
        {
            var order = new Order { Id = 4, CustomerEmail = "test@mail.com", TotalAmount = 50 };
            _databaseMock.Setup(x => x.IsConnected).Returns(true);
            _databaseMock.Setup(x => x.Save(It.IsAny<Order>())).Throws(new Exception());

            var result = _processor.ProcessOrder(order);

            Assert.False(result);
            Assert.False(order.IsProcessed);
        }
    }
}

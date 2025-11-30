// Scripts/cart.js
$(document).ready(function () {
    // Xử lý nút "Thêm vào giỏ" trên mọi trang có .add-to-cart-btn
    $(document).on('click', '.add-to-cart-btn', function (e) {
        e.preventDefault();
        e.stopPropagation();

        var btn = $(this);
        var productId = btn.data('product-id');
        var productName = btn.data('product-name');
        var originalText = btn.html();

        btn.prop('disabled', true);
        btn.html('<i class="fa-solid fa-spinner fa-spin"></i> Đang thêm...');

        $.ajax({
            url: '/Cart/AddToCartAjax',
            type: 'POST',
            data: {
                productId: productId,
                quantity: 1
            },
            success: function (response) {
                if (response.success) {
                    // Cập nhật số lượng giỏ hàng trên header
                    $('.cart-count').text('(' + response.cartCount + ')');

                    showCartNotification(response.message, 'success');

                    btn.html('<i class="fa-solid fa-check"></i> Đã thêm');
                    setTimeout(function () {
                        btn.html(originalText);
                        btn.prop('disabled', false);
                    }, 1500);
                } else {
                    showCartNotification(response.message || 'Không thể thêm sản phẩm.', 'error');
                    btn.html(originalText);
                    btn.prop('disabled', false);
                }
            },
            error: function () {
                showCartNotification('Có lỗi xảy ra. Vui lòng thử lại!', 'error');
                btn.html(originalText);
                btn.prop('disabled', false);
            }
        });
    });

    function showCartNotification(message, type) {
        var $note = $('<div class="cart-notification ' + type + '">' + message + '</div>');
        $('body').append($note);

        setTimeout(function () {
            $note.addClass('show');
        }, 50);

        setTimeout(function () {
            $note.removeClass('show');
            setTimeout(function () {
                $note.remove();
            }, 300);
        }, 3000);
    }
});

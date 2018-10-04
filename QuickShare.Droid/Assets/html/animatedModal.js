/*=========================================
 * animatedModal.js: Version 1.0
 * author: João Pereira
 * website: http://www.joaopereira.pt
 * email: joaopereirawd@gmail.com
 * Licensed MIT 
=========================================*/

(function ($) {
 
    $.fn.animatedModal = function(options) {
        var modal = $(this);

        //Defaults
        var settings = $.extend({
            modalTarget:'animatedModal', 
            position:'fixed', 
            width:'100%', 
            height:'100%', 
            top:'0px', 
            left:'0px', 
            zIndexIn: '9999',  
            zIndexOut: '-9999',  
            opacityIn:'1',  
            opacityOut:'0', 
            animatedIn:'zoomIn',
            animatedOut:'zoomOut',
            animationDuration:'.6s', 
            overflow:'auto', 
            // Callbacks
            beforeOpen: function() {},           
            afterOpen: function() {}, 
            beforeClose: function() {}, 
            afterClose: function() {}
 
            

        }, options);
        
        var closeBt = $('.close-'+settings.modalTarget);

        //console.log(closeBt)

        var href = $(modal).attr('href'),
            id = $('body').find('#'+settings.modalTarget),
            idConc = '#'+id.attr('id'),
            isOpened = false,
            isClosing = false;
            //console.log(idConc);
            // Default Classes
            id.addClass('animated');
            id.addClass(settings.modalTarget+'-off');

        //Init styles
        var initStyles = {
            'position':settings.position,
            'width':settings.width,
            'height':settings.height,
            'top':settings.top,
            'left':settings.left,
            'overflow-y':settings.overflow,
            'z-index':settings.zIndexOut,
            'opacity':settings.opacityOut,
            '-webkit-animation-duration':settings.animationDuration
        };
        //Apply stles
        id.css(initStyles);

        modal.click(function(event) {     
            console.log("opening:" + id.attr("id"));
            event.preventDefault();

            window.location.hash = href;
            isOpened = true;
            isClosing = false;

            $('body, html').css({'overflow':'hidden'});
            if (href == idConc) {
                if (id.hasClass(settings.modalTarget+'-off')) {
                    id.removeClass(settings.animatedOut);
                    id.removeClass(settings.modalTarget+'-off');
                    id.addClass(settings.modalTarget+'-on');
                } 

                 if (id.hasClass(settings.modalTarget+'-on')) {
                    settings.beforeOpen();
                    id.css({'opacity':settings.opacityIn,'z-index':settings.zIndexIn});
                    id.addClass(settings.animatedIn);  
                    id.one('webkitAnimationEnd mozAnimationEnd MSAnimationEnd oanimationend animationend', afterOpen);
                };  
            } 
        });

        function close() {
            $('body, html').css({'overflow':'auto'});

            settings.beforeClose(); //beforeClose

            isClosing = true;

            if (id.hasClass(settings.modalTarget+'-on')) {
                id.removeClass(settings.modalTarget+'-on');
                id.addClass(settings.modalTarget+'-off');
            } 

            if (id.hasClass(settings.modalTarget+'-off')) {
                id.removeClass(settings.animatedIn);
                id.addClass(settings.animatedOut);
                id.one('webkitAnimationEnd mozAnimationEnd MSAnimationEnd oanimationend animationend', afterClose);
            };
        }

        closeBt.click(function(event) {
            event.preventDefault();
            history.back();
        });

        function afterClose () {       
            console.log("afterClose:" + id.attr("id"));
            if (isOpened && isClosing) {
                id.css({'z-index':settings.zIndexOut});
                isClosing = false;
                isOpened = false;
            }
            settings.afterClose(); //afterClose
        }

        function afterOpen () {       
            settings.afterOpen(); //afterOpen
        }

        $(window).on('hashchange', function() {
            if (window.location.hash != href && isOpened) {
                close();
            }
        });

    }; // End animatedModal.js

}(jQuery));



        
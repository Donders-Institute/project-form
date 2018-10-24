"use strict";

jQuery(function($) {
    // Misc
    function generateUniqueIndex() {
        var uuid;

        do {
            uuid = "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, function(c) {
                var r = Math.random() * 16 | 0;
                var v = c === "x" ? r : r & 0x3 | 0x8;
                return v.toString(16);
            });
        } while ($("[data-index='" + uuid + "']").length > 0);

        return uuid;
    }

    function registerSubmitHandlers(root) {
        $(root).find("[data-submit-on]").each(function() {
            var $input = $(this);
            $input.on($input.data("submit-on"), function() {
                $("#form").submit();
            });
        });
    }

    function initializeUserList(userType, itemDefaults, createItem) {
        var $autocomplete = $(".aa-input[data-class='user-query'][data-usertype='" + userType + "']");
        var $userlist = $("[data-class='user-items'][data-usertype='" + userType + "']");

        $("[data-class='user-add'][data-usertype='" + userType + "']").click(function(event) {

            var id = $autocomplete.data("id");
            if (!id || $userlist.find("[data-id='" + id + "']").length > 0) {
                $autocomplete.addClass("border-danger");
                event.stopImmediatePropagation();
                return;
            }

            $userlist.append(createItem(Object.assign({
                index: generateUniqueIndex(),
                id: id,
                name: $autocomplete.autocomplete("val")
            }, itemDefaults)));

            $autocomplete.autocomplete("val", "");
            $autocomplete.trigger("input");
        });

        $userlist.children(":not(.list-group-placeholder)").replaceWith(function() {
            return createItem($(this).data());
        });
    }

    $(window).on("beforeunload", function() {
        var errorCount = $(".is-invalid").length;
        if (errorCount > 0) {
            return "The form contains input validation errors. Incorrect fields are not saved. Are you sure you want to leave the page?";
        }
        return undefined;
    });

    $("#form").data("submit-handler", function($form) {
        function updateErrors(errors) {
            $(".is-invalid").each(function() {
                Validation.clearError($(this));
            });

            Object.keys(errors).forEach(function(name) {
                var $inputs = $("[name='" + name + "']");
                if (errors[name].length > 0) {
                    $inputs.each(function() {
                        Validation.setError($(this), errors[name][0]);
                    });
                }
            });

//            $("[data-class='form-section']").each(function() {
//                var $section = $(this);
//                var count = $section.find(".is-invalid").length;
//                var $badge = $section.find("[data-class='error-count']");
//                if (count > 0) {
//                    $badge.removeClass("d-none").text(count + " error(s)");
//                } else {
//                    $badge.addClass("d-none");
//                }
//            });
        }

        if ($form.data("recently-submitted")) {
            $form.data("resubmit", true);
            return;
        }
        $form.data("recently-submitted", true);
        $.ajax({
            type: $form.attr("method"),
            url: $form.attr("action"),
            data: $form.serialize(),
            dataType: "json"
        }).done(function(data) {
            updateErrors(data || []);
        }).always(function() {
            setTimeout(function() {
                $form.data("recently-submitted", false);
                if ($form.data("resubmit")) {
                    $form.data("resubmit", false);
                    $form.submit();
                }
            }, 1000);
        });
    });

    $("#request-approval-modal").on("show.bs.modal", function(event) {
        var $button = $(event.relatedTarget);
        var section = $button.data("section");

        var $form = $(this).find("form");
        $form.prop("action", $form.data("url").replace("__SECTION__", encodeURIComponent(section)));
    });

    $("[data-class='radio-panel-input']").change(function() {
        var $input = $(this);
        if ($input.prop("checked")) {
            $($input.data("target")).collapse("show");
        }
    });

    $("[data-class='radio-panel-input']").each(function() {
        var $input = $(this);
        var $collapse = $($input.data("target"));
        $collapse.on("hide.bs.collapse", function() {
            $collapse.prop("disabled", true);
        });
        $collapse.on("show.bs.collapse", function() {
            $collapse.prop("disabled", false);
        });

        if ($input.prop("checked")) {
            $collapse.addClass("show");
        } else {
            $collapse.prop("disabled", true);
        }
    });



    // Ethics
    $("[data-class='ethics-approval']").change(function() {
        var $input = $(this);
        var text = "";
        var disabled = true;

        switch ($input.val()) {
        case "Blanket":
            text = "CMO2014/288";
            break;
        case "Children":
            text = "CMO2012/012";
            break;
        default:
            disabled = false;
        }

        $("[data-class='ethics-custom']")
            .val(text)
            .prop("disabled", disabled);
    }).change();



    // Experiment
    var labItemTemplate = $.templates("#lab-item-template");

    function createLabItem(data) {
        var $item = $(labItemTemplate.render(data));

        function updateTotalDuration() {
            var subjects = parseInt($item.find("[data-class='lab-item-subjects']").val());
            var sessions = parseInt($item.find("[data-class='lab-item-sessions']").val());
            var duration = parseInt($item.find("[data-class='lab-item-duration']").val());
            var totalDuration = moment.duration(subjects * sessions * duration, "minutes");
            $item.find("[data-class='lab-item-total-duration']").text(totalDuration.isValid() ? totalDuration.asHours().toFixed() + " hour(s)" : "");
        }

        $item.find("[data-class='remove-lab-item']").click(function() {
            $item.remove();
            updateLabItems();
            updateStandardQuota();
        });

        $item.find("[data-class='lab-item-subjects'],[data-class='lab-item-sessions'],[data-class='lab-item-duration']").change(updateTotalDuration);
        $item.find("[data-class='lab-item-subjects'],[data-class='lab-item-sessions']").change(updateStandardQuota);

        updateTotalDuration();
        updateStandardQuota();
        registerSubmitHandlers($item);

        return $item;
    }

    function updateLabItems() {
        var $container = $("[data-class='lab-items']");
        if ($container.children("[data-class='lab-item-expanded']").length === 0) {
            $("#quota-standard").prop("disabled", true);
            $("#quota-custom").prop("checked", true).change();
        } else {
            $("#quota-standard").prop("disabled", false);
        }
    }

    function updateStandardQuota() {
        var $items = $("[data-class='lab-item-expanded']");
        if ($items.length > 0) {
            var quota = 0;
            $items.each(function() {
                var $item = $(this);

                var subjects = parseInt($item.find("[data-class='lab-item-subjects']").val()) || 0;
                var sessions = parseInt($item.find("[data-class='lab-item-sessions']").val()) || 0;
                var sessionStorage = $item.data("storage-session");
                var fixedStorage = $item.data("storage-fixed");

                quota += subjects * sessions * sessionStorage + fixedStorage;
            });
            $("[data-class='quota-standard-value']").text(quota.toFixed(1) + " GB");
        } else {
            $("[data-class='quota-standard-value']").first().text("requires at least one lab");
        }
    }

    $("[data-class='add-lab-item']").click(function() {
        var $button = $(this);
        $("[data-class='lab-items']").append(createLabItem({
            index: generateUniqueIndex(),
            modality: $button.data("modality"),
            storageFixed: $button.data("storage-fixed"),
            storageSession: $button.data("storage-session")
        }));
        updateLabItems();
        updateStandardQuota();
    });

    $("[data-class='lab-item']").replaceWith(function() {
        return createLabItem($(this).data());
    });
    updateLabItems();
    updateStandardQuota();



    $("[data-class='user-query']").each(function() {
        var $autocomplete = $(this);

        $autocomplete.on("keypress", function(event) {
            if (event.key === "Enter") {
                $("[data-class='user-add'][data-usertype='" + $autocomplete.data("usertype") + "']").click();
                event.preventDefault();
            }
        });

        $autocomplete.on("input", function() {
            $autocomplete.removeClass("border-success").removeClass("border-danger");
            $autocomplete.removeData("id");
        });

        $autocomplete.on("autocomplete:selected", function(_event, suggestion) {
            $autocomplete.addClass("border-success");
            $autocomplete.data("id", suggestion.id);
        });

        $autocomplete.autocomplete({
                minLength: 2,
                highlight: true,
                autoselect: true
            },
            {
                source: function(query, callback) {
                    $.getJSON({
                        url: $autocomplete.data("url").replace("__QUERY__", encodeURIComponent(query))
                    }).done(function(data) {
                        callback(data);
                    });
                },
                display: function(suggestion) {
                    return $.fn.autocomplete.escapeHighlightedString(suggestion.name);
                },
                templates: {
                    suggestion: function(suggestion) {
                        return $("<div></div>")
                            .text(suggestion.name + " ")
                            .append($("<span></span>", { "class": "badge badge-info" }).text(suggestion.id))
                            .html();
                    },
                    empty: function(params) {
                        return $("<div></div>")
                            .text(" No results for '" + params.query + "'.")
                            .html();
                    }
                },
                debounce: 100
            }
        );
    });

    var experimenterItemTemplate = $.templates("#experimenter-item-template");
    initializeUserList("experimenter", {}, function(data) {
        var $item = $(experimenterItemTemplate.render(data));
        $item.find("[data-class='remove-experimenter']").click(function() {
            $(this).parents("[data-class='experimenter-item-expanded']").remove();
        });
        return $item;
    });



    // Data management
    var accessItemTemplate = $.templates("#access-item-template");
    initializeUserList("access", { role: "Viewer", canEdit: true, canRemove: true }, function(data) {
        var $item = $(accessItemTemplate.render(data));
            $item.find("[data-class='remove-access']").click(function() {
            $(this).parents("[data-class='access-item-expanded']").remove();
        });
        return $item;
    });



    $("[data-class='repository-user-warning']").each(function() {
        var $alert = $(this);
        $.getJSON({
            url: $alert.data("url")
        }).done(function(json) {
            if (json === false) {
                $alert.removeClass("d-none");
            }
        });
    });



    // Payment
    $("[data-class='cost-subjects'], [data-class='cost-average']").change(function() {
        var subjects = parseInt($("[data-class='cost-subjects']").val());
        var averageCost = parseFloat($("[data-class='cost-average']").val());
        var totalCost = subjects * averageCost;
        if (!isNaN(totalCost)) {
            $("[data-class='cost-predicted']").val(totalCost.toFixed(2));
        } else {
            $("[data-class='cost-predicted']").val("");
        }
    });

    $("[data-class='quota-overrule']").change(function() {
        var $this = $(this);
        if ($this.prop("checked")) {
            $("[data-class='quota-custom']").prop("disabled", false);
        } else {
            $("[data-class='quota-custom']").prop("disabled", true).val("");
        }
    });



    // Init
    registerSubmitHandlers(document);
});
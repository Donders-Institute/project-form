"use strict";

Initializers.register(function() {
    // Misc
    function registerSubmitHandlers(root) {
        $(root).find("[data-submit-on]").each(function() {
            var $input = $(this);
            var $section = $input.closest(".form-section");

            $input.on($input.data("submit-on"), function() {
                $section.submit();
            });
        });
    }

    function initializeUserList(userType, initialValues, itemDefaults, createItem) {
        var $autocomplete = $(".aa-input.user-query[data-usertype='" + userType + "']");
        var $userlist = $(".user-items[data-usertype='" + userType + "']");

        $(".user-add[data-usertype='" + userType + "']").click(function(event) {
            var id = $autocomplete.data("id");
            if (!id || $userlist.find("[data-id='" + id + "']").length > 0) {
                $autocomplete.addClass("border-danger");
                event.stopImmediatePropagation();
                return;
            }

            var $item = createItem(Object.assign({
                id: id,
                name: $autocomplete.autocomplete("val")
            }, itemDefaults));
            $userlist.append($item);
            registerSubmitHandlers($item);

            $autocomplete.autocomplete("val", "");
            $autocomplete.trigger("input");
        });

        initialValues.forEach(function(item) {
            $userlist.append(createItem(item));
        });
    }

//    $(window).on("beforeunload", function() {
//        var errorCount = $(".is-invalid").length;
//        if (errorCount > 0) {
//            return "The form contains input validation errors. Sections with incorrect fields are not saved. Are you sure you want to leave the page?";
//        }
//        return undefined;
//    });

    $(".form-section").data("submit-handler", function($section) {
        var $clickedElement = $(document.activeElement);
        if ($clickedElement.is("button[type='submit']")) {
            return;
        }

        $.ajax({
            type: $section.attr("method"),
            url: $section.attr("action"),
            data: $section.serialize(),
            dataType: "json"
        }).done(function(result) {
            $(".timestamp").val(result.timestamp);
            Validation.updateErrors(result.errors);
        });
    });

    $(".radio-panel-input").change(function() {
        var $input = $(this);
        if ($input.prop("checked")) {
            $($input.data("target")).collapse("show");
        }
    });

    $(".radio-panel-input").each(function() {
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

    // Funding
    $("#funding-contact-name").each(function() {
        var $autocomplete = $(this);

        $autocomplete.on("autocomplete:selected", function(_event, suggestion) {
            $("#funding-contact-email").val(suggestion.email);
        });

        $autocomplete.autocomplete({
                minLength: 2,
                highlight: true,
                autoselect: true
            },
            {
                source: function(query, callback) {
                    $.getJSON({
                        url: $autocomplete.data("url").replace(/__QUERY__/g, encodeURIComponent(query))
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
                            .text(suggestion.name)
                            .append($("<span></span>", { "class": "badge badge-info ml-1" }).text(suggestion.id))
                            .html();
                    },
                    empty: function(params) {
                        return $("<div></div>")
                            .text("No results for '" + params.query + "'.")
                            .html();
                    }
                },
                debounce: 100
            }
        );
    });



    // Ethics
    $("#ethics-approval").change(function() {
        var $option = $(this.selectedOptions[0]);
        var value = $option.data("value");
        if (value) {
            $("#ethics-approval-custom").val(value).prop("readonly", true);
        } else {
            $("#ethics-approval-custom").val("").prop("readonly", false);
        }
    });



    // Experiment
    var labItemTemplate = $.templates("#lab-item-template");

    function createLabItem(data) {
        var $item = $(labItemTemplate.render(data));

        function updateTotalDuration() {
            var subjects = parseInt($item.find(".lab-item-subjects").val());
            var sessions = parseInt($item.find(".lab-item-sessions").val());
            var duration = parseInt($item.find(".lab-item-duration").val());
            var totalDuration = moment.duration(subjects * sessions * duration, "minutes");
            $item.find(".lab-item-total-duration").text(totalDuration.isValid() ? totalDuration.asHours().toFixed() + " hour(s)" : "");
        }

        $item.find(".remove-lab-item").click(function() {
            var $button = $(this);
            var formData = $button.closest(".form-section").serializeArray().reduce(function(obj, entry) {
                if (entry.name === "Timestamp" || entry.name === "__RequestVerificationToken") {
                    obj[entry.name] = entry.value;
                }
                return obj;
            }, {});
            $.post({
                url: $button.attr("formaction"),
                data: formData,
                dataType: "json"
            }).done(function(result) {
                $(".timestamp").val(result.timestamp);
                $item.remove();
                updateStandardQuota();
            });
        });

        $item.find(".lab-item-subjects,.lab-item-sessions,.lab-item-duration").change(updateTotalDuration);
        $item.find(".lab-item-subjects,.lab-item-sessions").change(updateStandardQuota);

        updateTotalDuration();
        updateStandardQuota();

        return $item;
    }

    function updateStandardQuota() {
        var $container = $(".lab-items");
        var $items = $container.children(".lab-item");
        var quota = 0;
        var minimumQuota = parseInt($container.data("storage-minimum"));
        $items.each(function() {
            var $item = $(this);

            var subjects = parseInt($item.find(".lab-item-subjects").val()) || 0;
            var sessions = parseInt($item.find(".lab-item-sessions").val()) || 0;
            var storage = parseInt($item.data("storage")) || 0;
            quota += subjects * sessions * storage;
        });
        if (quota < minimumQuota) {
            quota = minimumQuota;
        }
        $(".quota-standard-value").text((quota / 1000).toFixed(1) + " GB");
    }

    $(".add-lab-item").click(function() {
        var $button = $(this);
        var formData = $button.closest(".form-section").serializeArray().reduce(function(obj, entry) {
            if (entry.name === "Timestamp" || entry.name === "__RequestVerificationToken") {
                obj[entry.name] = entry.value;
            }
            return obj;
        }, {});
        $.post({
            url: $button.attr("formaction"),
            dataType: "json",
            data: formData
        }).done(function(result) {
            $(".timestamp").val(result.timestamp);
            var $item = createLabItem({
                id: result.labId,
                modality: $button.data("modality"),
                storage: $button.data("storage")
            });
            $(".lab-items").append($item);
            registerSubmitHandlers($item);
        });
    });

    JSON.parse($("#labs").html()).forEach(function(item) {
        $(".lab-items").append(createLabItem(item));
    });
    updateStandardQuota();



    $(".user-query").each(function() {
        var $autocomplete = $(this);

        $autocomplete.on("keypress", function(event) {
            if (event.key === "Enter") {
                $(".user-add[data-usertype='" + $autocomplete.data("usertype") + "']").click();
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
            }, {
                source: function(query, callback) {
                    $.getJSON({
                        url: $autocomplete.data("url").replace(/__QUERY__/g, encodeURIComponent(query))
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
                            .text(suggestion.name)
                            .append($("<span></span>", { "class": "badge badge-info ml-1" }).text(suggestion.id))
                            .html();
                    },
                    empty: function(params) {
                        return $("<div></div>")
                            .text("No results for '" + params.query + "'.")
                            .html();
                    }
                },
                debounce: 100
            }
        );
    });

    var experimenterItemTemplate = $.templates("#experimenter-item-template");
    initializeUserList("experimenter", JSON.parse($("#experimenters").html()), {}, function(data) {
        var $item = $(experimenterItemTemplate.render(data));
        $item.find(".remove-experimenter").click(function() {
            $(this).closest(".experimenter-item-expanded").remove();
        });
        return $item;
    });



    // Data management
    var accessItemTemplate = $.templates("#access-item-template");
    initializeUserList("access", JSON.parse($("#access-rules").html()), { role: "Viewer", canEdit: true, canRemove: true }, function(data) {
        var $item = $(accessItemTemplate.render(data));
        $item.find(".remove-access").click(function() {
            $(this).closest(".access-item-expanded").remove();
        });
        return $item;
    });



    $(".repository-user-warning").each(function() {
        var $alert = $(this);
        $.getJSON({
            url: $alert.data("url")
        }).done(function(json) {
            if (json === false) {
                $alert.removeClass("d-none");
            }
        });
    });

    // Privacy
    $("#privacy-data-disposal-term").change(function() {
        var $option = $(this.selectedOptions[0]);
        var value = $option.data("value");
        if (value) {
            $("#privacy-data-disposal-term-custom").val(value).prop("readonly", true);
        } else {
            $("#privacy-data-disposal-term-custom").val("").prop("readonly", false);
        }
    });

//    $("#privacy-data-disposal-term-custom").each(function() {
//        var $input = $(this);
//        $input.css("height", $("#privacy-data-disposal-term").prop("height") + "px");
//    });


    $("#cost-average,#cost-max-total").change(function() {
        var $input = $(this);
        var val = parseFloat($input.val());
        if (!isNaN(val)) {
            $input.val(val.toFixed(2));
        }
    });

    // Payment
    $("#cost-subjects,#cost-average").change(function() {
        var subjects = parseInt($("#cost-subjects").val());
        var averageCost = parseFloat($("#cost-average").val());
        var totalCost = subjects * averageCost;
        $("#cost-predicted").val(isNaN(totalCost) ? "" : totalCost.toFixed(2));
    }).change();



    // Init
    registerSubmitHandlers(document);
});
angular.module("umbraco")
.controller("uTagsy.PropertyEditors.TagsController",
    
    function ($http, $rootScope, $scope, dialogService, entityResource, editorState, $log, assetsService, iconHelper) {
    //function ($rootScope, $scope, $log, assetsService) {
        assetsService
            .load([
                "/App_Plugins/uTagsy/propertyEditors/uTagsy.css"
            ]);
        
        // utagsy - get all tags from web api
        $scope.allTags = [];
        $scope.currentTags = [];
        $scope.currentTagIds = [];

        // get current node id by parsing window.location
        var idIndex = window.location.hash.lastIndexOf('/') + 1;
        var currentNodeId = window.location.hash.substr(idIndex, window.location.hash.length - idIndex);

        // get all tags in tree
        $http.get("/umbraco/backoffice/api/uTagsyApi/GetAllTags/?currentNodeId=" + currentNodeId)
             .success(function(data, status, headers, config) {
                $scope.allTags = data;
             });


        ///--
        /// Do web api call to get tag names from currentTagIds ($scope.model.value)
        $scope.initTagNamesFromIds = function () {
            $http.get("/umbraco/backoffice/api/uTagsyApi/GetTagNames/?nodeIds=" + $scope.model.value)
                 .success(function (data, status, headers, config) {
                     $scope.currentTags = data;
                 });
        };


        //load current value
        if ($scope.model.value) {
            $scope.currentTagIds = $scope.model.value.split(",");
            $scope.initTagNamesFromIds();
        }


        ///--
        /// Add tag on enter key press
        $scope.addTagOnKeyPress = function (e) {
            var code = e.keyCode || e.which;
            if (code == 13) { //Enter keycode   
                //this is required, otherwise the html form will attempt to submit.
                e.preventDefault();
                $scope.addTag();
            }
        };


        ///--
        /// Add tag to view
        $scope.addTag = function (e) {
            if ($scope.currentTags.indexOf($scope.tagToAdd) < 0) {
                $scope.currentTags.push($scope.tagToAdd);
            }
            $scope.tagToAdd = "";
        };


        ///--
        /// Remove tag from view
        $scope.removeTag = function (tag) {
            var i = $scope.currentTags.indexOf(tag);
            if (i >= 0) {
                $scope.currentTags.splice(i, 1);
            }
        };


        ///--
        /// Toggle all tags list
        $scope.showTags = function (e) {
            $('#uTagsy-tags-all').toggleClass('hide');
        };

        ///--
        /// Adds a tag from the "all tags list"
        $scope.addFromAllTags = function (e) {
            var tag = $(e.currentTarget).attr('rel');
            $scope.tagToAdd = tag;
            $scope.addTag(e);
        };
        
        
        //sync model on submit (needed since we convert an array to string)	
        $scope.$on("formSubmitting", function (ev, args) {
            $.ajax({
                url: "/umbraco/backoffice/api/uTagsyApi/GetAndEnsureNodeIdsForTags/?currentNodeId=" + currentNodeId + "&tags=" + $scope.currentTags,
                async:false
            }).done(function (data) {
                $scope.model.value = data.join();
                $scope.model.onValueChanged($scope.model.value);
            });
        });

        //vice versa
        $scope.model.onValueChanged = function (newVal, oldVal) {
            //update the display val again if it has changed from the server
            $scope.model.value = newVal;
            $scope.currentTagIds = $scope.model.value.split(",");
            $scope.initTagNamesFromIds();
            console.log($scope.model.value);
            console.log($scope.currentTagIds);
        };
    }
);


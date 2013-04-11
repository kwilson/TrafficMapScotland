namespace Ibi.JourneyPlanner.Web.Code
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Web.Hosting;

    using GeoJSON.Net;
    using GeoJSON.Net.Feature;
    using GeoJSON.Net.Geometry;

    using Ibi.JourneyPlanner.Web.Code.Language;
    using Ibi.JourneyPlanner.Web.Extensions;
    using Ibi.JourneyPlanner.Web.Models;
    using Ibi.JourneyPlanner.Web.Models.Exceptions;

    using OsmSharp.Osm;
    using OsmSharp.Osm.Core;
    using OsmSharp.Osm.Data.Core.Processor.Filter.Sort;
    using OsmSharp.Osm.Routing.Data.Processing;
    using OsmSharp.Osm.Routing.Interpreter;
    using OsmSharp.Routing;
    using OsmSharp.Routing.Core;
    using OsmSharp.Routing.Core.Graph.DynamicGraph.SimpleWeighed;
    using OsmSharp.Routing.Core.Graph.Memory;
    using OsmSharp.Routing.Core.Graph.Router.Dykstra;
    using OsmSharp.Routing.Graph.DynamicGraph.SimpleWeighed;
    using OsmSharp.Routing.Graph.Memory;
    using OsmSharp.Routing.Graph.Router.Dykstra;
    using OsmSharp.Routing.Instructions;
    using OsmSharp.Routing.Osm.Data.Processing;
    using OsmSharp.Routing.Osm.Interpreter;
    using OsmSharp.Tools.Math.Geo;

    public class Engine
    {
        private static Engine instance;

        private IRouter<RouterPoint> router;
        private OsmRoutingInterpreter interpreter;

        public static Engine Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Engine();
                }

                return instance;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Engine"/> class.
        /// </summary>
        protected Engine()
        {
            // keeps a memory-efficient version of the osm-tags.
            var tagsIndex = new OsmTagsIndex();

            // creates a routing interpreter. (used to translate osm-tags into a routable network)
            interpreter = new OsmRoutingInterpreter();

            // create a routing datasource, keeps all processed osm routing data.
            var osmData = new MemoryRouterDataSource<SimpleWeighedEdge>(tagsIndex);

            // load data into this routing datasource.
            var fileSource = HostingEnvironment.MapPath("~/App_Data/Manchester.osm.pbf");
            Stream osmXmlData = new FileInfo(fileSource).OpenRead(); // for example moscow!
            using (osmXmlData)
            {
                var targetData = new SimpleWeighedDataGraphProcessingTarget(
                                osmData,
                                interpreter,
                                osmData.TagsIndex,
                                VehicleEnum.Car);

                // replace this with PBFdataProcessSource when having downloaded a PBF file.
                var dataProcessorSource = new
                  OsmSharp.Osm.Data.PBF.Raw.Processor.PBFDataProcessorSource(osmXmlData);

                // pre-process the data.
                var sorter = new DataProcessorFilterSort();
                sorter.RegisterSource(dataProcessorSource);
                targetData.RegisterSource(sorter);
                targetData.Pull();
            }

            // create the router object: there all routing functions are available.
            router = new Router<SimpleWeighedEdge>(
                osmData,
                interpreter,
                new DykstraRoutingLive(osmData.TagsIndex));
        }

        /// <summary>
        /// Returns a value indicating whether the vehicle type is supported.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        /// <returns><c>true</c> if the vehicle is supported; otherwise <c>false</c>.</returns>
        public bool SupportsTransportMode(VehicleEnum vehicle)
        {
            return router.SupportsVehicle(vehicle);
        }

        public LocationModel GetNearestPointTo(VehicleEnum transportMode, double latitude, double longitude)
        {
            var point = router.Resolve(transportMode, new GeoCoordinate(latitude, longitude));
            return point == null 
                ? null 
                : new LocationModel(point.Location.Latitude, point.Location.Longitude);
        }

        /// <summary>
        /// Calculates the route.
        /// </summary>
        /// <param name="transportMode">The transport mode.</param>
        /// <param name="start">The start point.</param>
        /// <param name="end">The end point.</param>
        /// <returns>
        /// A new <see cref="RouteModel" /> with the route details.
        /// </returns>
        public RouteModel CalculatePointToPoint(VehicleEnum transportMode, GeoCoordinate start, GeoCoordinate end)
        {
            // calculate route.
            var startPoint = router.Resolve(transportMode, start);
            var endPoint = router.Resolve(transportMode, end);
            var route = router.Calculate(transportMode, startPoint, endPoint);

            if (route == null)
            {
                throw new RoutingException("No matching route found.");
            }

            var coordinates = route.Entries
                .Select(x => new GeographicPosition(x.Latitude, x.Longitude))
                .ToList();

            var lineString = new LineString(coordinates);

            var feature = new Feature(
                lineString,
                new Dictionary<string, object>
                    {
                        { "name", "Test route result." },
                        { "distance", route.TotalDistance },
                        { "journeytime", route.TotalTime },
                    });

            var generator = new InstructionGenerator();
            var instructions = generator.Generate(route, interpreter, new SimpleEnglishLanguageGenerator());

            return new RouteModel
                       {
                           Results = new ResultSet(feature)
                       };
        }
    }
}
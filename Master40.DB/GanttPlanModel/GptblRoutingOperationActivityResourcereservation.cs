﻿namespace Master40.DB.GanttPlanModel
{
    public partial class GptblRoutingOperationActivityResourcereservation
    {
        public string ClientId { get; set; }
        public string RoutingId { get; set; }
        public string OperationId { get; set; }
        public string AlternativeId { get; set; }
        public int SplitId { get; set; }
        public int ActivityId { get; set; }
        public string ReservationId { get; set; }
        public int ReservationType { get; set; }
        public int ResourceType { get; set; }
        public string ResourceId { get; set; }
    }
}

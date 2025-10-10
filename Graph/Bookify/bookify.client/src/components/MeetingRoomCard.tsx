import { Users, Monitor, Coffee, Wifi, CalendarDays } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";

interface MeetingRoom {
  id: string;
  name: string;
  capacity: number;
  amenities: string[];
  available: boolean;
  floor: number;
}

interface MeetingRoomCardProps {
  room: MeetingRoom;
  onBook: (roomId: string) => void;
  onViewBookings: (roomId: string) => void;
}

const amenityIcons: Record<string, typeof Monitor> = {
  "TV Screen": Monitor,
  "Coffee": Coffee,
  "WiFi": Wifi,
};

export const MeetingRoomCard = ({ room, onBook, onViewBookings }: MeetingRoomCardProps) => {
  return (
    <Card className="pixel-border hover:pixel-glow transition-all duration-300 bg-card overflow-hidden">
      <CardHeader className="pb-3">
        <div className="flex justify-between items-start mb-2">
          <CardTitle className="text-sm leading-relaxed">{room.name}</CardTitle>
          <Badge 
            variant={room.available ? "default" : "secondary"}
            className="pixel-border text-[10px] leading-relaxed"
          >
            {room.available ? "OPEN" : "BUSY"}
          </Badge>
        </div>
        <div className="flex items-center gap-2 text-muted-foreground">
          <Users className="h-4 w-4" />
          <span className="text-xs">{room.capacity} MAX</span>
        </div>
      </CardHeader>
      <CardContent className="space-y-3">
        <div className="flex flex-wrap gap-2">
          {room.amenities.map((amenity) => {
            const Icon = amenityIcons[amenity] || Monitor;
            return (
              <div
                key={amenity}
                className="flex items-center gap-1 bg-muted px-2 py-1 text-[10px]"
              >
                <Icon className="h-3 w-3" />
                <span>{amenity.toUpperCase()}</span>
              </div>
            );
          })}
        </div>
        <div className="text-xs text-muted-foreground">
          FLOOR {room.floor}
        </div>
        <div className="flex gap-2">
          <Button
            variant="outline"
            className="flex-1 pixel-border text-xs h-10"
            onClick={() => onViewBookings(room.id)}
          >
            <CalendarDays className="h-3 w-3 mr-1" />
            BOOKINGS
          </Button>
          <Button
            variant={room.available ? "default" : "secondary"}
            className="flex-1 pixel-border text-xs h-10"
            onClick={() => onBook(room.id)}
            disabled={!room.available}
          >
            {room.available ? "BOOK NOW" : "UNAVAILABLE"}
          </Button>
        </div>
      </CardContent>
    </Card>
  );
};

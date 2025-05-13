import { BattleDto } from "./battle-dto";
import { Friend } from "./friend";

export interface User {
    id : number,
    nickname : string,
    email : string,
    avatarPath : string,
    role: string,
    isInQueue: boolean,
    stateId: number,
    friend: Friend[],
    banned : boolean,
    battles: BattleDto[],
    totalPoints: number
}

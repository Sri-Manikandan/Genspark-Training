export class Product{
    constructor(public id: number, public name: string, public price: number, public description: string, public image: string) {
        this.id = id;
        this.name = name;
        this.price = price;
        this.description = description;
        this.image = image;
    }
}